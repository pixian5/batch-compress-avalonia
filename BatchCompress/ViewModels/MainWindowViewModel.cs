using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.Json;

namespace BatchCompress.ViewModels;

public class CompressionItem : ObservableObject
{
    private string _path = "";
    public string Path
    {
        get => _path;
        set => SetProperty(ref _path, value);
    }

    private string _type = "";
    public string Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public string DisplayName => $"{Type}: {System.IO.Path.GetFileName(Path)}";
}

public class Settings
{
    public List<string> SourcePaths { get; set; } = new();
    public List<string> FilePaths { get; set; } = new();
    public string SavePath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public bool UseRandomPassword { get; set; } = true;
    public bool SolidArchive { get; set; } = true;
    public bool SkipCompressed { get; set; } = true;
    public string ArchiveFormat { get; set; } = "7z";
    public string CompressionLevel { get; set; } = "正常";
    public string SplitSize { get; set; } = "";
}

public partial class MainWindowViewModel : ViewModelBase
{
    private BindingList<CompressionItem> _compressionItems = new();
    public BindingList<CompressionItem> CompressionItems
    {
        get => _compressionItems;
        set => SetProperty(ref _compressionItems, value);
    }

    private int _selectedCompressionItemIndex = -1;
    public int SelectedCompressionItemIndex
    {
        get => _selectedCompressionItemIndex;
        set => SetProperty(ref _selectedCompressionItemIndex, value);
    }

    public Window? CurrentWindow { get; set; }
    public Action? RefreshUI { get; set; }

    private string _savePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public string SavePath
    {
        get => _savePath;
        set => SetProperty(ref _savePath, value);
    }

    private bool _useRandomPassword = true;
    public bool UseRandomPassword
    {
        get => _useRandomPassword;
        set => SetProperty(ref _useRandomPassword, value);
    }

    private bool _solidArchive = true;
    public bool SolidArchive
    {
        get => _solidArchive;
        set => SetProperty(ref _solidArchive, value);
    }

    private bool _skipCompressed = true;
    public bool SkipCompressed
    {
        get => _skipCompressed;
        set => SetProperty(ref _skipCompressed, value);
    }

    private string _splitSize = "";
    public string SplitSize
    {
        get => _splitSize;
        set => SetProperty(ref _splitSize, value);
    }

    private string _password = "";
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public List<string> ArchiveFormats { get; } = new() { "7z", "zip", "rar", "tar", "gz", "bz2", "xz" };

    private string _archiveFormat = "7z";
    public string ArchiveFormat
    {
        get => _archiveFormat;
        set => SetProperty(ref _archiveFormat, value);
    }

    public List<string> CompressionLevels { get; } = new() { "无压缩", "快速", "正常", "最大", "极限" };

    private string _compressionLevel = "正常";
    public string CompressionLevel
    {
        get => _compressionLevel;
        set => SetProperty(ref _compressionLevel, value);
    }

    private string _logs = "日志信息";
    public string Logs
    {
        get => _logs;
        set => SetProperty(ref _logs, value);
    }

    private string _status = "就绪";
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    private double _progress = 0;
    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    private string _progressText = "";
    public string ProgressText
    {
        get => _progressText;
        set => SetProperty(ref _progressText, value);
    }

    private CancellationTokenSource? _cancellationTokenSource;
    public RelayCommand SelectSaveCommand { get; }
    public RelayCommand AddFolderCommand { get; }
    public RelayCommand AddFileCommand { get; }
    public RelayCommand ComputeMd5Command { get; }
    public RelayCommand RemoveCompressionItemCommand { get; }
    public RelayCommand ClearCompressionItemsCommand { get; }
    public AsyncRelayCommand TestArchiveCommand { get; }
    public RelayCommand AboutCommand { get; }
    public RelayCommand SaveSettingsCommand { get; }
    public RelayCommand ClearLogsCommand { get; }
    public AsyncRelayCommand CompressCommand { get; }
    public AsyncRelayCommand DecompressCommand { get; }
    public RelayCommand CancelCommand { get; }

    public MainWindowViewModel()
    {
        // 监听集合变化以确保UI更新
        CompressionItems.ListChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(CompressionItems));
        };

        SelectSaveCommand = new RelayCommand(SelectSave);
        AddFolderCommand = new RelayCommand(AddFolder);
        AddFileCommand = new RelayCommand(AddFile);
        ComputeMd5Command = new RelayCommand(ComputeMd5);
        RemoveCompressionItemCommand = new RelayCommand(RemoveCompressionItem);
        ClearCompressionItemsCommand = new RelayCommand(ClearCompressionItems);
        TestArchiveCommand = new AsyncRelayCommand(TestArchive);
        AboutCommand = new RelayCommand(About);
        SaveSettingsCommand = new RelayCommand(SaveSettings);
        ClearLogsCommand = new RelayCommand(ClearLogs);
        CompressCommand = new AsyncRelayCommand(Compress);
        DecompressCommand = new AsyncRelayCommand(Decompress);
        CancelCommand = new RelayCommand(Cancel);

        LoadSettings();
    }

    private async void SelectSave()
    {
        if (CurrentWindow == null)
        {
            Logs += "\n错误: 窗口引用为空";
            return;
        }

        try
        {
            var result = await CurrentWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择保存压缩文件的文件夹",
                AllowMultiple = false
            });
            
            if (result != null && result.Count > 0)
            {
                SavePath = result[0].Path.LocalPath;
                Logs += $"\n保存路径已设置为: {SavePath}";
            }
        }
        catch (Exception ex)
        {
            Logs += $"\n保存路径选择器错误: {ex.Message}";
        }
    }

    private async void AddFolder()
    {
        if (CurrentWindow == null)
        {
            Logs += "\n错误: 窗口引用为空";
            return;
        }

        try
        {
            Logs += "\n正在打开文件夹选择器...";
            
            var result = await CurrentWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择要压缩的文件夹",
                AllowMultiple = false
            });
            
            if (result != null && result.Count > 0)
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var item = new CompressionItem 
                    { 
                        Path = result[0].Path.LocalPath, 
                        Type = "文件夹" 
                    };
                    CompressionItems.Add(item);
                    Logs += $"\n已添加文件夹: {result[0].Path.LocalPath} (DisplayName: {item.DisplayName})";
                    Logs += $"\n当前列表项目数: {CompressionItems.Count}";
                    // 手动触发UI更新
                    OnPropertyChanged(nameof(CompressionItems));                    RefreshUI?.Invoke();                });
            }
            else
            {
                Logs += "\n未选择任何文件夹或选择器被取消";
            }
        }
        catch (Exception ex)
        {
            Logs += $"\n文件夹选择器错误: {ex.Message}";
        }
    }

    private async void AddFile()
    {
        if (CurrentWindow == null)
        {
            Logs += "\n错误: 窗口引用为空";
            return;
        }

        try
        {
            Logs += "\n正在打开文件选择器...";
            
            var result = await CurrentWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择要压缩的文件",
                AllowMultiple = true
            });
            
            Logs += $"\n文件选择器结果: {result?.Count ?? 0} 个文件";
            
            if (result != null && result.Count > 0)
            {
                Logs += "\n正在添加文件到列表...";
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var file in result)
                    {
                        var item = new CompressionItem 
                        { 
                            Path = file.Path.LocalPath, 
                            Type = "文件" 
                        };
                        CompressionItems.Add(item);
                        Logs += $"\n已添加文件: {file.Path.LocalPath} (DisplayName: {item.DisplayName})";
                    }
                    Logs += $"\n文件添加完成，总计: {CompressionItems.Count} 个项目";
                    // 强制UI更新
                    OnPropertyChanged(nameof(CompressionItems));
                });
            }
            else
            {
                Logs += "\n未选择任何文件或选择器被取消";
            }
        }
        catch (Exception ex)
        {
            Logs += $"\n文件选择器错误: {ex.Message}";
        }
    }

    private void ComputeMd5()
    {
        if (SelectedCompressionItemIndex >= 0 && SelectedCompressionItemIndex < CompressionItems.Count)
        {
            var item = CompressionItems[SelectedCompressionItemIndex];
            if (item.Type == "文件" && File.Exists(item.Path))
            {
                using var md5 = System.Security.Cryptography.MD5.Create();
                using var stream = File.OpenRead(item.Path);
                var hash = md5.ComputeHash(stream);
                var md5String = BitConverter.ToString(hash).Replace("-", "").ToLower();
                Logs += $"\nMD5 of {Path.GetFileName(item.Path)}: {md5String}";
            }
            else if (item.Type == "文件夹")
            {
                Logs += $"\n无法计算文件夹的MD5: {item.Path}";
            }
            else
            {
                Logs += $"\n文件不存在: {item.Path}";
            }
        }
        else
        {
            Logs += "\n请先选择一个文件";
        }
    }

    private async Task TestArchive()
    {
        Logs += "\n开始测试档案";
        Status = "测试中...";
        Progress = 0;
        ProgressText = "0%";
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Run(() => PerformTest(_cancellationTokenSource.Token));
            Status = "测试完成";
            Progress = 100;
            ProgressText = "100%";
        }
        catch (OperationCanceledException)
        {
            Logs += "\n测试已取消";
            Status = "已取消";
        }
        catch (Exception ex)
        {
            Logs += $"\n测试错误: {ex.Message}";
            Status = "错误";
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void About()
    {
        Logs += "\n批量压缩与解压工具\n版本 1.0\n基于Avalonia UI\n支持格式: 7z, zip, rar, tar, gz, bz2, xz\n功能: 压缩, 解压, 测试, MD5计算";
    }

    private void SaveSettings()
    {
        // 从CompressionItems中提取文件夹和文件路径
        var sourcePaths = new List<string>();
        var filePaths = new List<string>();
        
        foreach (var item in CompressionItems)
        {
            if (item.Type == "文件夹")
            {
                sourcePaths.Add(item.Path);
            }
            else if (item.Type == "文件")
            {
                filePaths.Add(item.Path);
            }
        }

        var settings = new Settings
        {
            SourcePaths = sourcePaths,
            FilePaths = filePaths,
            SavePath = SavePath,
            UseRandomPassword = UseRandomPassword,
            SolidArchive = SolidArchive,
            SkipCompressed = SkipCompressed,
            ArchiveFormat = ArchiveFormat,
            CompressionLevel = CompressionLevel,
            SplitSize = SplitSize
        };

        var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(settingsPath, json);
        Logs += "\n设置已保存";
    }

    private void ClearLogs()
    {
        Logs = "日志已清除";
    }

    private void RemoveCompressionItem()
    {
        if (SelectedCompressionItemIndex >= 0 && SelectedCompressionItemIndex < CompressionItems.Count)
        {
            CompressionItems.RemoveAt(SelectedCompressionItemIndex);
            SelectedCompressionItemIndex = -1;
        }
    }

    private void ClearCompressionItems()
    {
        CompressionItems.Clear();
    }

    private void LoadSettings()
    {
        var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        if (File.Exists(settingsPath))
        {
            try
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<Settings>(json);
                if (settings != null)
                {
                    // 从设置中恢复CompressionItems
                    CompressionItems.Clear();
                    foreach (var path in settings.SourcePaths)
                    {
                        CompressionItems.Add(new CompressionItem { Path = path, Type = "文件夹" });
                    }
                    foreach (var path in settings.FilePaths)
                    {
                        CompressionItems.Add(new CompressionItem { Path = path, Type = "文件" });
                    }
                    SavePath = settings.SavePath;
                    UseRandomPassword = settings.UseRandomPassword;
                    SolidArchive = settings.SolidArchive;
                    SkipCompressed = settings.SkipCompressed;
                    ArchiveFormat = settings.ArchiveFormat;
                    CompressionLevel = settings.CompressionLevel;
                    SplitSize = settings.SplitSize;
                }
            }
            catch
            {
                // Ignore errors
            }
        }
    }

    private async Task Compress()
    {
        Logs += "\n开始压缩";
        Status = "压缩中...";
        Progress = 0;
        ProgressText = "0%";
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Run(() => PerformCompression(_cancellationTokenSource.Token));
            Status = "压缩完成";
            Progress = 100;
            ProgressText = "100%";
        }
        catch (OperationCanceledException)
        {
            Logs += "\n压缩已取消";
            Status = "已取消";
        }
        catch (Exception ex)
        {
            Logs += $"\n压缩错误: {ex.Message}";
            Status = "错误";
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private async Task Decompress()
    {
        Logs += "\n开始解压";
        Status = "解压中...";
        Progress = 0;
        ProgressText = "0%";
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Run(() => PerformDecompression(_cancellationTokenSource.Token));
            Status = "解压完成";
            Progress = 100;
            ProgressText = "100%";
        }
        catch (OperationCanceledException)
        {
            Logs += "\n解压已取消";
            Status = "已取消";
        }
        catch (Exception ex)
        {
            Logs += $"\n解压错误: {ex.Message}";
            Status = "错误";
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void Cancel()
    {
        Logs += "\n操作取消";
        _cancellationTokenSource?.Cancel();
    }

    private void PerformCompression(CancellationToken token)
    {
        if (CompressionItems.Count == 0)
        {
            Logs += "\n没有待压缩项目";
            return;
        }

        var allSources = new List<string>();
        foreach (var item in CompressionItems)
        {
            if (item.Type == "文件夹" && Directory.Exists(item.Path))
            {
                allSources.Add(item.Path);
            }
            else if (item.Type == "文件" && File.Exists(item.Path))
            {
                allSources.Add(item.Path);
            }
        }

        if (allSources.Count == 0)
        {
            Logs += "\n没有有效的源";
            return;
        }

        foreach (var source in allSources)
        {
            if (token.IsCancellationRequested) break;

            var archivePath = Path.Combine(SavePath, $"archive_{Path.GetFileName(source)}_{DateTime.Now:yyyyMMdd_HHmmss}.{ArchiveFormat}");

            var password = UseRandomPassword ? GeneratePassword() : Password;
            if (!string.IsNullOrEmpty(password))
            {
                Logs += $"\n使用密码: {password}";
            }

            string fileName;
            string args;

            if (ArchiveFormat == "rar")
            {
                fileName = "rar";
                args = $"a \"{archivePath}\" \"{source}\" -r";
                if (!string.IsNullOrEmpty(password))
                {
                    args += $" -p{password}";
                }
                if (SolidArchive)
                {
                    args += " -s";
                }

                // Compression level for RAR
                var level = CompressionLevel switch
                {
                    "无压缩" => "0",
                    "快速" => "1",
                    "正常" => "3",
                    "最大" => "4",
                    "极限" => "5",
                    _ => "3"
                };
                args += $" -m{level}";

                if (!string.IsNullOrEmpty(SplitSize))
                {
                    args += $" -v{SplitSize}";
                }
            }
            else
            {
                fileName = "7z";
                args = $"a \"{archivePath}\" \"{source}\" -r";
                if (!string.IsNullOrEmpty(password))
                {
                    args += $" -p{password}";
                }
                if (SolidArchive)
                {
                    args += " -ms=on";
                }

                // Compression level
                var level = CompressionLevel switch
                {
                    "无压缩" => "0",
                    "快速" => "1",
                    "正常" => "5",
                    "最大" => "7",
                    "极限" => "9",
                    _ => "5"
                };
                args += $" -mx{level}";

                // For different formats, 7z handles most
                if (ArchiveFormat == "zip")
                {
                    args += " -tzip";
                }
                else if (ArchiveFormat == "tar")
                {
                    args += " -ttar";
                }
                else if (ArchiveFormat == "gz")
                {
                    args += " -tgzip";
                }
                else if (ArchiveFormat == "bz2")
                {
                    args += " -tbzip2";
                }
                else if (ArchiveFormat == "xz")
                {
                    args += " -txz";
                }

                if (!string.IsNullOrEmpty(SplitSize))
                {
                    args += $" -v{SplitSize}";
                }
            }

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process != null)
            {
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Logs += $"\n压缩完成: {archivePath}";
                }
                else
                {
                    Logs += $"\n压缩失败: {process.StandardError.ReadToEnd()}";
                }
            }
            else
            {
                Logs += $"\n无法启动{fileName}，请确保已安装并在PATH中";
            }
        }

        Progress = 100;
        ProgressText = "100%";
    }

    private void PerformDecompression(CancellationToken token)
    {
        var extensions = new[] { "*.7z", "*.zip", "*.rar", "*.tar", "*.gz", "*.bz2", "*.xz" };
        
        // 从CompressionItems中获取所有文件夹
        var sourceFolders = CompressionItems.Where(item => item.Type == "文件夹" && Directory.Exists(item.Path))
                                          .Select(item => item.Path)
                                          .ToList();

        foreach (var sourcePath in sourceFolders)
        {
            if (!Directory.Exists(sourcePath)) continue;

            var archives = new List<string>();
            foreach (var ext in extensions)
            {
                archives.AddRange(Directory.GetFiles(sourcePath, ext, SearchOption.AllDirectories));
            }

            foreach (var archivePath in archives)
            {
                if (token.IsCancellationRequested) break;

                var password = UseRandomPassword ? GeneratePassword() : Password;
                string fileName;
                string args;

                var ext = Path.GetExtension(archivePath).ToLower();
                if (ext == ".rar")
                {
                    fileName = "rar";
                    args = $"x \"{archivePath}\" \"{SavePath}\" -y";
                    if (!string.IsNullOrEmpty(password))
                    {
                        args += $" -p{password}";
                    }
                }
                else
                {
                    fileName = "7z";
                    args = $"x \"{archivePath}\" -o\"{SavePath}\" -y";
                    if (!string.IsNullOrEmpty(password))
                    {
                        args += $" -p{password}";
                    }
                }

                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process != null)
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        Logs += $"\n解压完成: {Path.GetFileName(archivePath)}";
                    }
                    else
                    {
                        Logs += $"\n解压失败: {process.StandardError.ReadToEnd()}";
                    }
                }
                else
                {
                    Logs += $"\n无法启动{fileName}";
                }
            }
        }

        Progress = 100;
        ProgressText = "100%";
    }

    private void PerformTest(CancellationToken token)
    {
        var extensions = new[] { "*.7z", "*.zip", "*.rar", "*.tar", "*.gz", "*.bz2", "*.xz" };
        var archives = new List<string>();
        
        // 从CompressionItems中获取所有文件夹
        var sourceFolders = CompressionItems.Where(item => item.Type == "文件夹" && Directory.Exists(item.Path))
                                          .Select(item => item.Path)
                                          .ToList();

        foreach (var sourcePath in sourceFolders)
        {
            if (!Directory.Exists(sourcePath)) continue;

            foreach (var ext in extensions)
            {
                archives.AddRange(Directory.GetFiles(sourcePath, ext, SearchOption.AllDirectories));
            }
        }

        foreach (var archivePath in archives)
        {
            if (token.IsCancellationRequested) break;

            var password = UseRandomPassword ? GeneratePassword() : Password;
            string fileName;
            string args;

            var ext = Path.GetExtension(archivePath).ToLower();
            if (ext == ".rar")
            {
                fileName = "rar";
                args = $"t \"{archivePath}\"";
                if (!string.IsNullOrEmpty(password))
                {
                    args += $" -p{password}";
                }
            }
            else
            {
                fileName = "7z";
                args = $"t \"{archivePath}\"";
                if (!string.IsNullOrEmpty(password))
                {
                    args += $" -p{password}";
                }
            }

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process != null)
            {
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Logs += $"\n测试通过: {Path.GetFileName(archivePath)}";
                }
                else
                {
                    Logs += $"\n测试失败: {process.StandardError.ReadToEnd()}";
                }
            }
            else
            {
                Logs += $"\n无法启动{fileName}";
            }
        }

        Progress = 100;
        ProgressText = "100%";
    }

    private bool IsAlreadyCompressed(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLower();
        return ext == ".zip" || ext == ".rar" || ext == ".7z" || ext == ".gz" || ext == ".bz2" || ext == ".xz";
    }

    private string GeneratePassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
