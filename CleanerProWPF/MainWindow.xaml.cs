using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CleanerProWPF
{
    public partial class MainWindow : Window
    {
        private List<TaskItem> tasks = new List<TaskItem>();
        private Dictionary<int, long> scanResults = new Dictionary<int, long>();

        public MainWindow()
        {
            InitializeComponent();
            CheckAdminRights();
            InitializeTasks();
            UpdateSelectedCount();
            UpdateTotalSize();
            UpdateSystemStatus();
        }

        private void CheckAdminRights()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                bool isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

                if (!isAdmin)
                {
                    MessageBox.Show(
                        "Для корректной работы программы требуются права администратора.\n" +
                        "Некоторые системные папки не будут доступны для сканирования.",
                        "Предупреждение",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch { }
        }

        private void UpdateSystemStatus()
        {
            try
            {
                var drives = DriveInfo.GetDrives();
                var systemDrive = drives.FirstOrDefault(d => d.Name == Environment.SystemDirectory.Substring(0, 3));

                string statusText = "Отлично";
                string statusColor = "#2ab674";
                string statusIcon = "🛡️";
                string details = "Система чиста и оптимизирована";

                if (systemDrive != null && systemDrive.IsReady)
                {
                    var totalSpace = systemDrive.TotalSize;
                    var freeSpace = systemDrive.TotalFreeSpace;
                    var usedSpace = totalSpace - freeSpace;
                    var freePercent = (double)freeSpace / totalSpace * 100;
                    var usedPercent = (double)usedSpace / totalSpace * 100;

                    if (freeSpace < 5L * 1024 * 1024 * 1024)
                    {
                        statusText = "Критично!";
                        statusColor = "#ef4444";
                        statusIcon = "⚠️";
                        details = $"Свободно {FormatSize(freeSpace)} из {FormatSize(totalSpace)} ({freePercent:F0}%)";
                    }
                    else if (freeSpace < 20L * 1024 * 1024 * 1024)
                    {
                        statusText = "Требуется внимание";
                        statusColor = "#f59e0b";
                        statusIcon = "🔶";
                        details = $"Свободно {FormatSize(freeSpace)} из {FormatSize(totalSpace)} ({freePercent:F0}%)";
                    }
                    else if (usedPercent > 85)
                    {
                        statusText = "Хорошо";
                        statusColor = "#2ab674";
                        statusIcon = "✅";
                        details = $"Свободно {FormatSize(freeSpace)} из {FormatSize(totalSpace)} ({freePercent:F0}%)";
                    }
                    else
                    {
                        statusText = "Отлично";
                        statusColor = "#2ab674";
                        statusIcon = "🛡️";
                        details = $"Свободно {FormatSize(freeSpace)} из {FormatSize(totalSpace)} ({freePercent:F0}%)";
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    var statusBox = FindName("SystemStatusBox") as StackPanel;
                    if (statusBox != null)
                    {
                        statusBox.Children.Clear();

                        var header = new TextBlock
                        {
                            Text = "Состояние системы",
                            FontSize = 12,
                            Foreground = (Brush)FindResource("TextSecondary")
                        };
                        statusBox.Children.Add(header);

                        var valueBlock = new TextBlock
                        {
                            FontSize = 18,
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 2, 0, 0)
                        };
                        var converter = new BrushConverter();
                        valueBlock.Foreground = (Brush)converter.ConvertFromString(statusColor);
                        valueBlock.Inlines.Add(new Run(statusIcon + " "));
                        valueBlock.Inlines.Add(new Run(statusText));
                        statusBox.Children.Add(valueBlock);

                        var detailsBlock = new TextBlock
                        {
                            Text = details,
                            FontSize = 11,
                            Foreground = (Brush)FindResource("TextSecondary")
                        };
                        statusBox.Children.Add(detailsBlock);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления статуса: {ex.Message}");
            }
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.#} {sizes[order]}";
        }

        private void InitializeTasks()
        {
            var iconColors = new[]
            {
                "#3164fa", "#2ab674", "#8150eb", "#f59e0b",
                "#2ab674", "#ef4444", "#14b8a6", "#f59e0b", "#8150eb"
            };

            var icons = new[] { "👤", "🖥️", "📦", "⚡", "👁️", "💻", "🌐", "📂", "🧹" };

            tasks = new List<TaskItem>
            {
                new TaskItem { Id = 1, Name = "Временные файлы пользователя (%TEMP%)",
                    Path = Environment.GetEnvironmentVariable("TEMP") ?? "%TEMP%",
                    Description = "Удаление временных файлов из папки пользователя",
                    Icon = icons[0], IconColor = iconColors[0],
                    Status = "ожидает анализа", IsChecked = true },
                new TaskItem { Id = 2, Name = "Системные временные файлы (Windows\\Temp)",
                    Path = @"C:\Windows\Temp",
                    Description = "Очистка системной папки временных файлов",
                    Icon = icons[1], IconColor = iconColors[1],
                    Status = "ожидает анализа", IsChecked = true },
                new TaskItem { Id = 3, Name = "Кэш обновлений (SoftwareDistribution\\Download)",
                    Path = @"C:\Windows\SoftwareDistribution\Download",
                    Description = "Удаление загруженных файлов обновлений Windows",
                    Icon = icons[2], IconColor = iconColors[2],
                    Status = "ожидает анализа", IsChecked = true },
                new TaskItem { Id = 4, Name = "Prefetch (ускоритель запуска)",
                    Path = @"C:\Windows\Prefetch",
                    Description = "Очистка данных для ускорения запуска приложений",
                    Icon = icons[3], IconColor = iconColors[3],
                    Status = "ожидает анализа", IsChecked = true },
                new TaskItem { Id = 5, Name = "Кэш шейдеров NVIDIA",
                    Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NVIDIA", "GLCache"),
                    Description = "Очистка кэша шейдеров видеокарты NVIDIA",
                    Icon = icons[4], IconColor = iconColors[4],
                    Status = "ожидает анализа", IsChecked = true },
                new TaskItem { Id = 6, Name = "Кэш шейдеров AMD",
                    Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AMD", "ShaderCache"),
                    Description = "Очистка кэша шейдеров видеокарты AMD",
                    Icon = icons[5], IconColor = iconColors[5],
                    Status = "ожидает анализа", IsChecked = true },
                new TaskItem { Id = 7, Name = "Сброс DNS-кэша",
                    Path = "cmd://ipconfig /flushdns",
                    Description = "Очистка кэша DNS для обновления сетевых данных",
                    Icon = icons[6], IconColor = iconColors[6],
                    Status = "Готово к очистке", IsChecked = true, IsReady = true },
                new TaskItem { Id = 8, Name = "Очистка истории Проводника",
                    Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Recent"),
                    Description = "Удаление истории поиска и списка последних файлов",
                    Icon = icons[7], IconColor = iconColors[7],
                    Status = "ожидает анализа", IsChecked = true },
                new TaskItem { Id = 9, Name = "Запуск очистки диска (cleanmgr)",
                    Path = "cmd://cleanmgr",
                    Description = "Запуск встроенной утилиты очистки диска Windows",
                    Icon = icons[8], IconColor = iconColors[8],
                    Status = "Готово к очистке", IsChecked = true, IsReady = true }
            };

            RenderTasks();
        }

        private void RenderTasks()
        {
            TasksPanel.Children.Clear();
            foreach (var task in tasks)
            {
                var border = new Border
                {
                    Style = (Style)FindResource("TaskItemStyle"),
                    Tag = task.Id,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new ScaleTransform(1, 1)
                };
                border.MouseLeftButtonDown += (s, e) => OpenPath(task.Path);

                // Анимация для строк задач
                border.MouseEnter += (s, e) =>
                {
                    var scaleTransform = border.RenderTransform as ScaleTransform;
                    if (scaleTransform != null)
                    {
                        var animX = new DoubleAnimation(1.01, TimeSpan.FromSeconds(0.12)) { DecelerationRatio = 0.7 };
                        var animY = new DoubleAnimation(1.01, TimeSpan.FromSeconds(0.12)) { DecelerationRatio = 0.7 };
                        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
                        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
                    }
                };
                border.MouseLeave += (s, e) =>
                {
                    var scaleTransform = border.RenderTransform as ScaleTransform;
                    if (scaleTransform != null)
                    {
                        var animX = new DoubleAnimation(1, TimeSpan.FromSeconds(0.12)) { DecelerationRatio = 0.7 };
                        var animY = new DoubleAnimation(1, TimeSpan.FromSeconds(0.12)) { DecelerationRatio = 0.7 };
                        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
                        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
                    }
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var checkBox = new CheckBox
                {
                    IsChecked = task.IsChecked,
                    Tag = task.Id,
                    Margin = new Thickness(0, 0, 12, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White
                };
                checkBox.Checked += (s, e) => { task.IsChecked = true; UpdateSelectedCount(); UpdateTotalSize(); };
                checkBox.Unchecked += (s, e) => { task.IsChecked = false; UpdateSelectedCount(); UpdateTotalSize(); };
                Grid.SetColumn(checkBox, 0);
                grid.Children.Add(checkBox);

                var iconBorder = new Border
                {
                    Width = 44,
                    Height = 44,
                    CornerRadius = new CornerRadius(10),
                    Margin = new Thickness(0, 0, 14, 0)
                };

                try { iconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(task.IconColor)); }
                catch { iconBorder.Background = new SolidColorBrush(Color.FromRgb(49, 100, 250)); }

                var iconText = new TextBlock
                {
                    Text = task.Icon,
                    FontSize = 20,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                iconBorder.Child = iconText;
                Grid.SetColumn(iconBorder, 1);
                grid.Children.Add(iconBorder);

                var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                infoStack.Children.Add(new TextBlock
                {
                    Text = task.Name,
                    FontSize = 15,
                    FontWeight = FontWeights.Medium,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 2)
                });

                var pathText = new TextBlock
                {
                    Text = $"📂 {task.Path}",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(47, 107, 255)),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                pathText.MouseLeftButtonDown += (s, e) => { e.Handled = true; OpenPath(task.Path); };
                infoStack.Children.Add(pathText);

                infoStack.Children.Add(new TextBlock
                {
                    Text = task.Description,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(138, 148, 166)),
                    Margin = new Thickness(0, 2, 0, 0)
                });

                Grid.SetColumn(infoStack, 2);
                grid.Children.Add(infoStack);

                var statusStack = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 12, 0),
                    MinWidth = 100
                };

                var sizeText = new TextBlock
                {
                    Text = "0 МБ",
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Tag = $"Size_{task.Id}"
                };
                statusStack.Children.Add(sizeText);

                var statusText = new TextBlock
                {
                    Text = task.Status,
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Tag = $"Status_{task.Id}"
                };

                if (task.IsReady)
                {
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(42, 182, 116));
                    statusText.FontWeight = FontWeights.Medium;
                }
                else if (task.Status == "ожидает анализа")
                {
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(138, 148, 166));
                }
                else
                {
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(42, 182, 116));
                }
                statusStack.Children.Add(statusText);

                Grid.SetColumn(statusStack, 3);
                grid.Children.Add(statusStack);

                var arrow = new TextBlock
                {
                    Text = "›",
                    FontSize = 22,
                    Foreground = new SolidColorBrush(Color.FromRgb(138, 148, 166)),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 0)
                };
                arrow.MouseLeftButtonDown += (s, e) => { e.Handled = true; OpenPath(task.Path); };
                Grid.SetColumn(arrow, 4);
                grid.Children.Add(arrow);

                border.Child = grid;
                TasksPanel.Children.Add(border);
            }
        }

        private void UpdateSelectedCount()
        {
            var total = tasks.Count;
            var checkedCount = tasks.Count(t => t.IsChecked);
            SelectedCount.Text = $"{checkedCount} из {total}";
        }

        private void UpdateTotalSize()
        {
            long total = 0;
            foreach (var task in tasks)
            {
                if (task.IsChecked && scanResults.TryGetValue(task.Id, out long size))
                {
                    total += size;
                }
            }

            if (total > 0)
                TotalSizeText.Text = $"Всего: {total / 1024.0 / 1024.0:F1} МБ";
            else
                TotalSizeText.Text = "Всего: 0 МБ";
        }

        private void OpenPath(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return;

                if (path.StartsWith("cmd://"))
                {
                    var cmd = path.Replace("cmd://", "");
                    System.Diagnostics.Process.Start("cmd", $"/c {cmd}");
                    MessageBox.Show($"Выполнено: {cmd}", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (Directory.Exists(path))
                {
                    System.Diagnostics.Process.Start("explorer.exe", path);
                }
                else
                {
                    MessageBox.Show($"Путь не найден: {path}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SocialButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                var url = button.Tag.ToString();
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void AnalyzeBtn_Click(object sender, RoutedEventArgs e)
        {
            AnalyzeBtn.IsEnabled = false;
            AnalyzeBtnText.Text = "🔄 Сканирование...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                scanResults.Clear();
                foreach (var task in tasks.Where(t => !t.IsReady))
                {
                    long size = 0;
                    string resolvedPath = task.Path;

                    if (resolvedPath.Contains("%"))
                    {
                        resolvedPath = Environment.ExpandEnvironmentVariables(resolvedPath);
                    }

                    if (Directory.Exists(resolvedPath))
                    {
                        try
                        {
                            size = GetDirectorySize(resolvedPath);
                            System.Diagnostics.Debug.WriteLine($"Task {task.Id} ({task.Name}): {size} bytes");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка при сканировании {resolvedPath}: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Папка не существует: {resolvedPath}");
                    }
                    scanResults[task.Id] = size;
                }
            });

            Dispatcher.Invoke(() =>
            {
                foreach (var task in tasks)
                {
                    if (scanResults.TryGetValue(task.Id, out long size))
                    {
                        var sizeText = FindSizeTextBlock(task.Id);
                        if (sizeText != null)
                        {
                            if (size > 0)
                            {
                                sizeText.Text = $"{size / 1024.0 / 1024.0:F1} МБ";
                            }
                            else
                            {
                                sizeText.Text = "0 МБ";
                            }
                        }

                        var statusText = FindStatusTextBlock(task.Id);
                        if (statusText != null && !task.IsReady)
                        {
                            if (size > 0)
                            {
                                statusText.Text = "можно очистить";
                                statusText.Foreground = new SolidColorBrush(Color.FromRgb(42, 182, 116));
                            }
                            else
                            {
                                statusText.Text = "чисто";
                                statusText.Foreground = new SolidColorBrush(Color.FromRgb(138, 148, 166));
                            }
                        }
                    }
                }

                var totalSize = scanResults.Values.Sum() / 1024.0 / 1024.0;
                UpdateTotalSize();

                AnalyzeBtnText.Text = "🔍 АНАЛИЗ";
                AnalyzeBtn.IsEnabled = true;

                MessageBox.Show($"✅ Анализ завершён!\nНайдено {totalSize:F1} МБ для очистки",
                               "Результат анализа", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private TextBlock FindSizeTextBlock(int taskId)
        {
            foreach (var child in TasksPanel.Children)
            {
                var border = child as Border;
                if (border != null && border.Tag != null && (int)border.Tag == taskId)
                {
                    var grid = border.Child as Grid;
                    if (grid != null && grid.Children.Count > 3)
                    {
                        var statusStack = grid.Children[3] as StackPanel;
                        if (statusStack != null && statusStack.Children.Count > 0)
                        {
                            return statusStack.Children[0] as TextBlock;
                        }
                    }
                }
            }
            return null;
        }

        private TextBlock FindStatusTextBlock(int taskId)
        {
            foreach (var child in TasksPanel.Children)
            {
                var border = child as Border;
                if (border != null && border.Tag != null && (int)border.Tag == taskId)
                {
                    var grid = border.Child as Grid;
                    if (grid != null && grid.Children.Count > 3)
                    {
                        var statusStack = grid.Children[3] as StackPanel;
                        if (statusStack != null && statusStack.Children.Count > 1)
                        {
                            return statusStack.Children[1] as TextBlock;
                        }
                    }
                }
            }
            return null;
        }

        private long GetDirectorySize(string path)
        {
            long size = 0;
            try
            {
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        size += fi.Length;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в GetDirectorySize: {ex.Message}");
            }
            return size;
        }

        private async void CleanBtn_Click(object sender, RoutedEventArgs e)
        {
            var selected = tasks.Where(t => t.IsChecked && !t.IsReady).ToList();
            if (!selected.Any())
            {
                MessageBox.Show("Не выбрано ни одной категории для очистки", "Информация",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                "Вы уверены, что хотите очистить выбранные категории?\nЭто действие нельзя отменить.",
                "Подтверждение очистки",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            CleanBtn.IsEnabled = false;
            CleanBtnText.Text = "🧹 Очистка...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                foreach (var task in selected)
                {
                    try
                    {
                        if (task.Path.StartsWith("cmd://"))
                        {
                            var cmd = task.Path.Replace("cmd://", "");
                            System.Diagnostics.Process.Start("cmd", $"/c {cmd}");
                            continue;
                        }

                        string resolvedPath = task.Path;
                        if (resolvedPath.Contains("%"))
                        {
                            resolvedPath = Environment.ExpandEnvironmentVariables(resolvedPath);
                        }

                        if (Directory.Exists(resolvedPath))
                        {
                            long size = 0;
                            var files = Directory.GetFiles(resolvedPath, "*", SearchOption.AllDirectories);
                            foreach (var file in files)
                            {
                                try
                                {
                                    size += new FileInfo(file).Length;
                                    File.Delete(file);
                                }
                                catch { }
                            }

                            Dispatcher.Invoke(() =>
                            {
                                var sizeText = FindSizeTextBlock(task.Id);
                                if (sizeText != null) sizeText.Text = "0 МБ";

                                var statusText = FindStatusTextBlock(task.Id);
                                if (statusText != null)
                                {
                                    statusText.Text = "✅ Очищено";
                                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(42, 182, 116));
                                }
                                scanResults[task.Id] = 0;
                                UpdateTotalSize();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"Ошибка при очистке {task.Name}:\n{ex.Message}",
                                          "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                }
            });

            CleanBtnText.Text = "🧹 ОЧИСТИТЬ";
            CleanBtn.IsEnabled = true;

            MessageBox.Show("✅ Очистка завершена!", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                this.DragMove();
        }

        private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class TaskItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public string IconColor { get; set; } = "#3164fa";
        public string Status { get; set; } = "";
        public string Size { get; set; } = "0 МБ";
        public bool IsChecked { get; set; }
        public bool IsReady { get; set; }
    }
}