using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LyricEditor.UserControls
{
    /// <summary>
    /// RollingTextBlock.xaml 的交互逻辑
    /// </summary>
    public partial class RollingTextBlock : UserControl
    {
        public TextBlock currentTextBlock = null;
        public RollingTextBlock()
        {
            InitializeComponent();
            Loaded += TopMiddle;
        }
        private void TopMiddle(object sender, RoutedEventArgs e)
        {
            if (this.currentTextBlock != null)
            {
                //使字垂直居中
                FontFamily fontFamily = currentTextBlock.FontFamily;
                double fontDpiSize = currentTextBlock.FontSize;
                double fontHeight = Math.Ceiling(fontDpiSize * fontFamily.LineSpacing);
                Top = (this.ActualHeight - fontHeight) / 2;
            }
        }

        public override void OnApplyTemplate()
        {
            try
            {
                base.OnApplyTemplate();
                currentTextBlock = this.GetTemplateChild("textBlock") as TextBlock;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        #region Dependency Properties
        public static DependencyProperty TextProperty =
           DependencyProperty.Register("Text", typeof(string), typeof(RollingTextBlock),
           new PropertyMetadata(""));

        public static DependencyProperty LeftProperty =
           DependencyProperty.Register("Left", typeof(double), typeof(RollingTextBlock), new PropertyMetadata(0D));

        public static DependencyProperty TopProperty =
           DependencyProperty.Register("Top", typeof(double), typeof(RollingTextBlock), new PropertyMetadata(0D));

        #endregion

        #region Public Variables
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set {SetValue(TextProperty, value);}
        }

        public double Left
        {
            get { return (double)GetValue(LeftProperty); }
            set { SetValue(LeftProperty, value); }
        }

        public double Top
        {
            get { return (double)GetValue(TopProperty); }
            set { SetValue(TopProperty, value); }
        }

        #endregion

        
    }
}
