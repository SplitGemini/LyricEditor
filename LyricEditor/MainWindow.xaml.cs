using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Reflection;
using System.Configuration;
using LyricEditor.UserControls;
using LyricEditor.Lyric;
using Win32 = System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LyricEditor
{
    public enum LrcPanelType
    {
        LrcLinePanel,
        LrcTextPanel
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer Timer;
        public MainWindow()
        {
            InitializeComponent();
            LrcLinePanel = (LrcLineView)LrcPanelContainer.Content;
            LrcLinePanel.MyMainWindow = this;
            LrcTextPanel = new LrcTextView();
            LrcTextPanel.MyMainWindow = this;

            Timer = new DispatcherTimer();
            Timer.Tick += new EventHandler(Timer_Tick);
            Timer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            Timer.Start();
        }

        #region 成员变量

        private LrcPanelType CurrentLrcPanel = LrcPanelType.LrcLinePanel;
        /// <summary>
        /// 表示播放器是否正在播放
        /// </summary>
        private bool isPlaying = false;

        private LrcLineView LrcLinePanel;
        private LrcTextView LrcTextPanel;

        public LrcManager Manager
        {
            get => LrcLinePanel.Manager;
        }

        public TimeSpan ShortTimeShift { get; private set; } = new TimeSpan(0, 0, 2);
        public TimeSpan LongTimeShift { get; private set; } = new TimeSpan(0, 0, 5);

        private string FileName;

        private static string[] MediaExtensions = new string[] { ".mp3", ".wav", ".3gp", ".mp4", ".avi", ".wmv", ".wma", ".aac", ".flac", ".ape", ".opus", ".ogg" };
        private static string[] LyricExtensions = new string[] { ".lrc", ".txt" };
        private static string TempFileName = "temp.txt";

        #endregion
        #region 计时器
        private bool isRight = true;
        private bool canRoll = false;
        private double nextTextWidth;

        /// <summary>
        /// 每个计时器时刻，更新时间轴上的全部信息
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!IsMediaAvailable) return;
            
            var current = MediaPlayer.Position;
            CurrentTimeText.Text = $"{current.Minutes:00}:{current.Seconds:00}";

            TimeBackground.Value = MediaPlayer.Position.TotalSeconds / MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            var value = Manager.GetNearestLrc(MediaPlayer.Position);
            var shouldSetToLeft = false;
            if (value != null)
            {
                string tmp = string.Empty;
                
                value.TryGetValue("current", out tmp);
                if (!CurrentLrcText.Text.Equals(tmp))
                {
                    shouldSetToLeft = true;
                    if(!canRoll)
                        CurrentLrcText.Left = (CurrentLrcText.ActualWidth - nextTextWidth) / 2;
                }else
                {
                    shouldSetToLeft = false;
                    if (!canRoll)
                        CurrentLrcText.Left = (CurrentLrcText.ActualWidth - CurrentLrcText.currentTextBlock.ActualWidth) / 2;
                }
                CurrentLrcText.Text = tmp;
                value.TryGetValue("pre", out tmp);
                PreLrcText.Text = tmp;
                value.TryGetValue("next", out tmp);
                NextLrcText.Text = tmp;
                value.TryGetValue("afternext", out tmp);
                AfterNextLrcText.Text = tmp;
                value.TryGetValue("short", out tmp);
                if (tmp.Equals("true"))
                {
                    nextTextWidth = AfterNextLrcText.ActualWidth;
                }
                else {
                    nextTextWidth = NextLrcText.ActualWidth;
                }           
                
            }
            canRoll = CurrentLrcText.currentTextBlock.ActualWidth > CurrentLrcText.ActualWidth;
            if (canRoll)
            {
                var offset = 10.0;  //滚动左右空余位置
                double rollingInterval = (CurrentLrcText.currentTextBlock.ActualWidth - CurrentLrcText.ActualWidth)/ 50;  //每一步的偏移量，根据每段长度
                if (shouldSetToLeft)
                {
                    CurrentLrcText.Left = offset;
                }          
                else if (Math.Abs(CurrentLrcText.Left) <= CurrentLrcText.currentTextBlock.ActualWidth - CurrentLrcText.ActualWidth + offset && isRight)
                {
                    CurrentLrcText.Left -= rollingInterval;
                }
                /* 需要回滚取消注释
                else if (CurrentLrcText.Left <= offset)
                {
                    CurrentLrcText.Left += rollingInterval;
                    isRight = CurrentLrcText.Left >= offset;
                }
                */
            }  
        }

        #endregion

        #region 媒体播放器

        private bool IsMediaAvailable
        {
            get
            {
                
                if (MediaPlayer.Source is null) return false;
                if(MediaPlayer.HasAudio && MediaPlayer.NaturalDuration.HasTimeSpan)
                {
                    return true;
                } else
                {
                    Console.WriteLine(MediaPlayer.Source is null ? "player is null" : "player isn't null" + (MediaPlayer.HasAudio ? ", has audio" : ", don't has audio") + (MediaPlayer.NaturalDuration.HasTimeSpan ? ", has time span" : ", don't has time span"));
                    return false;
                }
            }
        }

        private void Play()
        {
            if (!IsMediaAvailable)
            {
                Console.WriteLine("media is not availible");
                MessageBox.Show("未加载音频");
                return;
            }

            MediaPlayer.Play();

            var image = (Image)PlayButton.Content;
            image.Source = new BitmapImage(new Uri("Icons/MediaButtonIcons/Pause.png", UriKind.RelativeOrAbsolute));
            image.Margin = new Thickness(0);

            isPlaying = true;
        }
        private void Pause()
        {
            if (!IsMediaAvailable) return;

            MediaPlayer.Pause();

            var image = (Image)PlayButton.Content;
            image.Source = new BitmapImage(new Uri("Icons/MediaButtonIcons/Start.png", UriKind.RelativeOrAbsolute));
            image.Margin = new Thickness(2, 0, 0, 0);

            isPlaying = false;
        }
        private void Stop()
        {
            if (!IsMediaAvailable) return;
            MediaPlayer.Position = TimeSpan.Zero;
            MediaPlayer.Stop();

            var image = (Image)PlayButton.Content;
            image.Source = new BitmapImage(new Uri("Icons/MediaButtonIcons/Start.png", UriKind.RelativeOrAbsolute));
            image.Margin = new Thickness(3, 0, 0, 0);

            isPlaying = false;
        }

        #endregion

        #region 内部方法

        private BitmapImage GetAlbumArt(string filename)
        {
            var file = TagLib.File.Create(filename);
            var bin = file.Tag.Pictures[0].Data.Data;
            file.Dispose();
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = new MemoryStream(bin);
            image.EndInit();

            return image;
        }
        private void GetLyric(string filename)
        {
            var lyricname = string.Empty;
            foreach (var ext in MediaExtensions)
            {
                if (filename.EndsWith(ext))
                {
                    lyricname = filename.Replace(ext, ".lrc");
                    break;
                }
            }
            if (!lyricname.Equals(string.Empty) && File.Exists(lyricname))
            {
                if (Manager.LoadFromText(File.ReadAllText(lyricname)))
                {
                    UpdateLrcView();
                }
                else
                {
                    LrcTextPanel.Text = Manager.text;
                    SwitchToTextLrcPanel();
                }
                
            } else if (filename.EndsWith(".mp3"))   //一般只有mp3采用id2tag
            {
                var file = TagLib.File.Create(filename);
                var lyric = file.Tag.Lyrics;
                file.Dispose();
                if (lyric != null)
                {
                    if (Manager.LoadFromText(lyric))
                    {
                        UpdateLrcView();
                    }
                    else
                    {
                        LrcTextPanel.Text = Manager.text;
                        SwitchToTextLrcPanel();
                    } 
                }
            }
        }

        private void LoadFromFile(string filename)
        {
            
            if (Manager.LoadFromFile(filename))
            {
                UpdateLrcView();
            }
            else
            {
                LrcTextPanel.Text = Manager.text;
                SwitchToTextLrcPanel();
            }
        }

        private void LoadFromText(string text)
        {
            if (Manager.LoadFromText(text))
            {
                UpdateLrcView();
            }
            else
            {
                LrcTextPanel.Text = Manager.text;
                SwitchToTextLrcPanel();
            }
        }

        private void UpdateLrcView()
        {
            LrcLinePanel.UpdateLrcPanel();
            LrcTextPanel.Text = Manager.ToString();
        }
        private void ImportMedia(string filename)
        {
            try
            {
                MediaPlayer.Source = new Uri(filename);
                MediaPlayer.Stop();
                Title = "歌词编辑器 ：" + Path.GetFileNameWithoutExtension(filename);
                Cover.Source = GetAlbumArt(filename);
                GetLyric(filename);
                CurrentLrcText.Text = string.Empty;
                PreLrcText.Text = string.Empty;
                NextLrcText.Text = string.Empty;
            }
            catch (Exception e)
            {
                Cover.Source = new BitmapImage(new Uri("Icons/disc.png", UriKind.Relative));
                Console.WriteLine("import media:" + e.Message);
            }
        }

        #endregion

        #region 菜单按钮

        /// <summary>
        /// 界面读取，用于初始化
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title = "歌词编辑器";
            
            // 上方时间轴的初始化
            CurrentLrcText.Text = string.Empty;
            PreLrcText.Text = string.Empty;
            NextLrcText.Text = string.Empty;
            TotalTimeText.Text = string.Empty;
            CurrentTimeText.Text = string.Empty;
            TimeBackground.Value = 0;

            // 读取配置文件
         
            // 退出时自动缓存
            AutoSaveTemp.IsChecked = bool.Parse(ConfigurationManager.AppSettings["AutoSaveTemp"]);
            // 导出 UTF-8
            ExportUTF8.IsChecked = bool.Parse(ConfigurationManager.AppSettings["ExportUTF8"]);
            // 时间取近似值
            LrcLinePanel.ApproxTime = ApproxTime.IsChecked = bool.Parse(ConfigurationManager.AppSettings["ApproxTime"]);
            // 时间偏差（改变 Text 会触发 TextChanged 事件，下同）
            TimeOffset.Text = ConfigurationManager.AppSettings["TimeOffset"];
            //LrcLinePanel.TimeOffset = new TimeSpan(0, 0, 0, 0, -(int)double.Parse(TimeOffset.Text));
            // 快进快退
            ShortShift.Text = ConfigurationManager.AppSettings["ShortTimeShift"];
            //ShortTimeShift = new TimeSpan(0, 0, 0, int.Parse(ShortShift.Text));
            LongShift.Text = ConfigurationManager.AppSettings["LongTimeShift"];
            //LongTimeShift = new TimeSpan(0, 0, 0, int.Parse(LongShift.Text));

            // 打开缓存文件
            if (AutoSaveTemp.IsChecked && File.Exists(TempFileName))
            {
                LoadFromFile(TempFileName);
            }
        }
        /// <summary>
        /// 程序退出，关闭计时器，修改配置文件
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            Timer.Stop();

            // 保存配置文件
            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfa.AppSettings.Settings["AutoSaveTemp"].Value = AutoSaveTemp.IsChecked.ToString();
            cfa.AppSettings.Settings["ExportUTF8"].Value = ExportUTF8.IsChecked.ToString();
            cfa.AppSettings.Settings["ApproxTime"].Value = LrcLinePanel.ApproxTime.ToString();
            cfa.AppSettings.Settings["TimeOffset"].Value = (-LrcLinePanel.TimeOffset.TotalMilliseconds).ToString();
            cfa.AppSettings.Settings["ShortTimeShift"].Value = ShortTimeShift.TotalSeconds.ToString();
            cfa.AppSettings.Settings["LongTimeShift"].Value = LongTimeShift.TotalSeconds.ToString();

            cfa.Save();

            // 保存缓存
            if (AutoSaveTemp.IsChecked)
            {
                Encoding encoding = ExportUTF8.IsChecked ? Encoding.UTF8 : Encoding.Default;
                using (var sw = new StreamWriter(new FileStream(TempFileName, FileMode.Create), encoding))
                {
                    sw.Write(Manager.ToString());
                }
            }
            else if (File.Exists(TempFileName))
            {
                File.Delete(TempFileName);
            }
        }

        /// <summary>
        /// 导入音频文件
        /// </summary>
        private void ImportMedia_Click(object sender, RoutedEventArgs e)
        {
            Win32.OpenFileDialog ofd = new Win32.OpenFileDialog();
            ofd.Filter = "媒体文件|*.mp3;*.wav;*.3gp;*.mp4;*.avi;*.wmv;*.wma;*.aac;*.flac;*.ape;*.opus;*.ogg|所有文件|*.*";

            if (ofd.ShowDialog() == Win32.DialogResult.OK)
            {
                ImportMedia(ofd.FileName);
                FileName = ofd.FileName;
            }
        }
        /// <summary>
        /// 导入歌词文件
        /// </summary>
        private void ImportLyric_Click(object sender, RoutedEventArgs e)
        {
            Win32.OpenFileDialog ofd = new Win32.OpenFileDialog();
            ofd.Filter = "歌词文件|*.lrc;*.txt|所有文件|*.*";

            if (ofd.ShowDialog() == Win32.DialogResult.OK)
            {
                LoadFromFile(ofd.FileName);
                FileName = ofd.FileName;
            }
        }
        /// <summary>
        /// 将歌词保存为文本文件
        /// </summary>
        private void ExportLyric_Click(object sender, RoutedEventArgs e)
        {
            Win32.SaveFileDialog ofd = new Win32.SaveFileDialog();
            ofd.Filter = "歌词文件|*.lrc|文本文件|*.txt|所有文件|*.*";

            if (!string.IsNullOrEmpty(FileName))
            {
                ofd.FileName = System.IO.Path.GetFileNameWithoutExtension(FileName);
            }

            if (ofd.ShowDialog() == Win32.DialogResult.OK)
            {
                Encoding encoding = ExportUTF8.IsChecked ? Encoding.UTF8 : Encoding.Default;
                using (var sw = new StreamWriter(new FileStream(ofd.FileName, FileMode.Create), encoding))
                {
                    switch (CurrentLrcPanel)
                    {
                        case LrcPanelType.LrcLinePanel:
                            sw.Write(Manager.ToString());
                            break;

                        case LrcPanelType.LrcTextPanel:
                            sw.Write(LrcTextPanel.LrcTextPanel.Text);
                            break;
                    }
                }
            }
            
            
        }
        /// <summary>
        /// 从剪贴板粘贴歌词文本
        /// </summary>
        private void ImportLyricFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            LoadFromText(Clipboard.GetText());
        }
        /// <summary>
        /// 将歌词文本复制到剪贴板
        /// </summary>
        private void ExportLyricToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Manager.ToString());
        }

        /// <summary>
        /// 配置选项发生变化
        /// </summary>
        private void Settings_Checked(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            switch (item.Name)
            {
                case "ApproxTime":
                    LrcLinePanel.ApproxTime = item.IsChecked;
                    break;
            }
        }

        /// <summary>
        /// 打开媒体文件后，更新时间轴上的总时间
        /// </summary>
        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            var totalTime = MediaPlayer.NaturalDuration.TimeSpan;
            TotalTimeText.Text = $"{totalTime.Minutes:00}:{totalTime.Seconds:00}";
            CurrentTimeText.Text = "00:00";
            Pause();
        }

        private void MediaPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Console.WriteLine(e.ErrorException);
        }
        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        /// <summary>
        /// 处理窗口的文件拖入事件
        /// </summary>
        public void Window_Drop(object sender, DragEventArgs e)
        {
            string[] filePath = ((string[])e.Data.GetData(DataFormats.FileDrop));

            foreach (var file in filePath)
            {
                string ext = System.IO.Path.GetExtension(file).ToLower();
                if (MediaExtensions.Contains(ext))
                {
                    ImportMedia(file);
                    FileName = file;
                }
                else if (LyricExtensions.Contains(ext))
                {
                    LoadFromFile(file);
                    FileName = file;
                }
            }
        }
        public void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Link;
            else
                e.Effects = DragDropEffects.None;
        }

        /// <summary>
        /// 关闭按钮
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TimeOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (LrcLinePanel is null) return;
            if (int.TryParse(TimeOffset.Text, out int offset))
            {
                LrcLinePanel.TimeOffset = new TimeSpan(0, 0, 0, 0, -offset);
            }
        }
        private void TimeShift_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (LrcLinePanel is null) return;

            TextBox box = sender as TextBox;
            if (int.TryParse(box.Text, out int value))
            {
                switch (box.Name)
                {
                    case "ShortShift":
                        ShortTimeShift = new TimeSpan(0, 0, value);
                        break;

                    case "LongShift":
                        LongTimeShift = new TimeSpan(0, 0, value);
                        break;
                }
            }
        }

        /// <summary>
        /// 重置所有歌词行的时间
        /// </summary>
        private void ResetAllTime_Click(object sender, RoutedEventArgs e)
        {
            LrcLinePanel.ResetAllTime();
        }
        /// <summary>
        /// 平移所有歌词行的时间
        /// </summary>
        private void ShiftAllTime_Click(object sender, RoutedEventArgs e)
        {
            string str = InputBox.Show(this, "请输入时间偏移量(ms)：");
            if (double.TryParse(str, out double offset))
            {
                LrcLinePanel.ShiftAllTime(new TimeSpan(0, 0, 0, 0, (int)(offset)));
            }
        }

        #endregion

        #region 工具按钮

        /// <summary>
        /// 切换面板
        /// </summary>
        private void SwitchLrcPanel_Click(object sender, RoutedEventArgs e)
        {
            switch (CurrentLrcPanel)
            {
                // 切换回纯文本
                case LrcPanelType.LrcLinePanel:
                    UpdateLrcView();
                    SwitchToTextLrcPanel();
                    break;

                // 切换回歌词行
                case LrcPanelType.LrcTextPanel:
                    // 在回到歌词行模式前，要检查当前文本能否进行正确转换
                    if (!Manager.LoadFromText(LrcTextPanel.Text))
                    {
                        return;
                    }
                    UpdateLrcView();
                    LrcPanelContainer.Content = LrcLinePanel;
                    CurrentLrcPanel = LrcPanelType.LrcLinePanel;
                    ToolsForLrcLineOnly.Visibility = Visibility.Visible;
                    FlagButton.Visibility = Visibility.Visible;
                    // 切换到line编辑模式时，按钮旋转角度复原，且相关按钮可用
                    ((Image)(SwitchButton).Content).LayoutTransform = new RotateTransform(0);
                    ClearAllTime.IsEnabled = false;
                    SortTime.IsEnabled = true;
                    ResetAllTime.IsEnabled = true;
                    ShiftAllTime.IsEnabled = true;
                    TranslateButton.IsEnabled = true;
                    TranslateSwitchButton.IsEnabled = true;
                    break;
            }
        }

        private void SwitchToTextLrcPanel() {
            if (CurrentLrcPanel == LrcPanelType.LrcTextPanel) return;
            LrcPanelContainer.Content = LrcTextPanel;
            CurrentLrcPanel = LrcPanelType.LrcTextPanel;
            ToolsForLrcLineOnly.Visibility = Visibility.Collapsed;
            FlagButton.Visibility = Visibility.Hidden;
            // 切换到text编辑模式时
            ((Image)SwitchButton.Content).LayoutTransform = new RotateTransform(180);
            ClearAllTime.IsEnabled = true;
            SortTime.IsEnabled = false;
            ResetAllTime.IsEnabled = false;
            ShiftAllTime.IsEnabled = false;
            TranslateButton.IsEnabled = false;
            TranslateSwitchButton.IsEnabled = false;
        }

        /// <summary>
        /// 播放按钮
        /// </summary>
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlaying)
            {
                Play();
            }
            // 否则反之
            else
            {
                Pause();
            }
        }
        /// <summary>
        /// 停止按钮
        /// </summary>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }
        /// <summary>
        /// 快进快退
        /// </summary>
        private void TimeShift_Click(object sender, RoutedEventArgs e)
        {
            if (!IsMediaAvailable) return;

            switch (((Button)sender).Name)
            {
                case "ShortShiftLeft":
                    MediaPlayer.Position -= ShortTimeShift;
                    break;
                case "ShortShiftRight":
                    MediaPlayer.Position += ShortTimeShift;
                    break;
                case "LongShiftLeft":
                    MediaPlayer.Position -= LongTimeShift;
                    break;
                case "LongShiftRight":
                    MediaPlayer.Position += LongTimeShift;
                    break;
            }
        }
        /// <summary>
        /// 时间轴点击
        /// </summary>
        private void TimeClickBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsMediaAvailable) return;

            double current = e.GetPosition(TimeClickBar).X;
            double percent = current / TimeClickBar.ActualWidth;
            TimeBackground.Value = percent;

            MediaPlayer.Position = new TimeSpan(0, 0, 0, 0, (int)(MediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds * percent));
        }
        /// <summary>
        /// 撤销
        /// </summary>
        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            switch (CurrentLrcPanel)
            {
                case LrcPanelType.LrcLinePanel:
                    LrcLinePanel.Undo();
                    break;

                case LrcPanelType.LrcTextPanel:
                    LrcTextPanel.LrcTextPanel.Undo();
                    break;
            }
        }
        /// <summary>
        /// 重做
        /// </summary>
        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            switch (CurrentLrcPanel)
            {
                case LrcPanelType.LrcLinePanel:
                    LrcLinePanel.Redo();
                    break;

                case LrcPanelType.LrcTextPanel:
                    LrcTextPanel.LrcTextPanel.Redo();
                    break;
            }
        }
        /// <summary>
        /// 将媒体播放位置应用到当前歌词行
        /// </summary>
        private void SetTime_Click(object sender, RoutedEventArgs e)
        {
            if (!IsMediaAvailable) return;
            if (CurrentLrcPanel != LrcPanelType.LrcLinePanel) return;

            LrcLinePanel.SetCurrentLineTime(MediaPlayer.Position);
        }
        /// <summary>
        /// 添加新行
        /// </summary>
        private void AddNewLine_Click(object sender, RoutedEventArgs e)
        {
            LrcLinePanel.AddNewLine(MediaPlayer.Position);
        }
        /// <summary>
        /// 删除行
        /// </summary>
        private void DeleteLine_Click(object sender, RoutedEventArgs e)
        {
            LrcLinePanel.DeleteLine();
        }
        /// <summary>
        /// 上移一行
        /// </summary>
        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            LrcLinePanel.MoveUp();
        }
        /// <summary>
        /// 下移一行
        /// </summary>
        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            LrcLinePanel.MoveDown();
        }
        /// <summary>
        /// 清空所有时间标记
        /// </summary>
        private void ClearAllTime_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentLrcPanel != LrcPanelType.LrcTextPanel) return;
            LrcTextPanel.ClearAllTime();
        }
        /// <summary>
        /// 强制排序
        /// </summary>
        private void SortTime_Click(object sender, RoutedEventArgs e)
        {
            Manager.Sort();
            LrcLinePanel.UpdateLrcPanel();
        }
        /// <summary>
        /// 清空全部内容
        /// </summary>
        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            switch (CurrentLrcPanel)
            {
                case LrcPanelType.LrcLinePanel:
                    Manager.Clear();
                    LrcLinePanel.UpdateLrcPanel();
                    break;

                case LrcPanelType.LrcTextPanel:
                    LrcTextPanel.Clear();
                    break;
            }
        }
        /// <summary>
        /// 软件信息
        /// </summary>
        private void Info_Click(object sender, RoutedEventArgs e)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LyricEditor.info.txt");
            using (StreamReader sr = new StreamReader(stream))
            {
                MessageBox.Show(sr.ReadToEnd(), "软件相关", MessageBoxButton.OKCancel);
            }
        }

        private void Translate_Click(object sender, RoutedEventArgs e)
        {
            if (!IsMediaAvailable) return;
            if (CurrentLrcPanel != LrcPanelType.LrcLinePanel) return;

            LrcLinePanel.SetCurrentLineTimeForTranslate();
        }

        private void TranslateSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentLrcPanel != LrcPanelType.LrcLinePanel) return;
            if (TranslateSwitchButton.IsChecked == true)
            {
                LrcLinePanel.LrcLinePanel.SelectionMode = SelectionMode.Multiple;
            }
            else {
                LrcLinePanel.LrcLinePanel.SelectionMode = SelectionMode.Single;
            }
        }
        #endregion

        #region 快捷键

        private void SetTimeShortcut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetTime_Click(this, null);
        }

        private void HelpShortcut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Info_Click(this, null);
        }

        private void PlayShortcut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PlayButton_Click(this, null);
        }

        private void UndoShortcut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Undo_Click(this, null);
        }

        private void RedoShortcut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Redo_Click(this, null);
        }

        private void InsertShortcut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (CurrentLrcPanel == LrcPanelType.LrcLinePanel)
            {
                AddNewLine_Click(this, null);
            }
        }


        private void Jump1Shortcut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MediaPlayer.Position += ShortTimeShift;
        }

        private void Jump2Shortcut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MediaPlayer.Position += LongTimeShift;
        }

        private void Rewind1Shortcut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MediaPlayer.Position -= ShortTimeShift;
        }

        private void Rewind2Shortcut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MediaPlayer.Position -= LongTimeShift;
        }

        private void StopShortcut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Stop();
        }

        private void SwitchShortcut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SwitchLrcPanel_Click(this, null);
        }

        private void TranslateShortcut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Translate_Click(this, null);
        }


        //Delete, Up, Down快捷键处理在LrcLineView.xaml.cs实现
        #endregion

        
    }
}