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

namespace WellboreViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // RealSense
        PXCMSenseManager psm;
        PXCMTouchlessController ptc;

        //private
        float oldXpos = 0.0F;
        float oldYpos = 0.0F;

        public MainWindow()
        {
            InitializeComponent();

            WellboresList.ItemsSource = new List<string>() { "Wellbore1","Wellbore2","Wellbore3","Wellbore4"};
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StartRealSense();

            UpdateConfiguration();

            StartFrameLoop();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopRealSense();
        }

        private void StartRealSense()
        {
            Console.WriteLine("Starting Touchless Controller");

            pxcmStatus rc;

            // creating Sense Manager
            psm = PXCMSenseManager.CreateInstance();
            Console.WriteLine("Creating SenseManager: " + psm == null ? "failed" : "success");
            if (psm == null)
            {
                MessageBox.Show("Failed to create SenseManager!", "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }

            // work from file if a filename is given as command line argument
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                psm.captureManager.SetFileName(args[1], false);
            }

            // Enable touchless controller in the multimodal pipeline
            rc = psm.EnableTouchlessController(null);
            Console.WriteLine("Enabling Touchless Controller: " + rc.ToString());
            if (rc != pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                MessageBox.Show("Failed to enable touchless controller!", "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }

            // initialize the pipeline
            PXCMSenseManager.Handler handler = new PXCMSenseManager.Handler();
            rc = psm.Init(handler);
            Console.WriteLine("Initializing the pipeline: " + rc.ToString());
            if (rc != pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                MessageBox.Show("Failed to initialize the pipeline!", "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }

            // getting touchless controller
            ptc = psm.QueryTouchlessController();
            if (ptc == null)
            {
                MessageBox.Show("Failed to get touchless controller!", "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }
            ptc.SubscribeEvent(new PXCMTouchlessController.OnFiredUXEventDelegate(OnTouchlessControllerUXEvent));
         
        }

        private void StopRealSense()
        {
            Console.WriteLine("Disposing SenseManager and Touchless Controller");
            ptc.Dispose();
            psm.Close();
            psm.Dispose();
        }

        private void OnTouchlessControllerUXEvent(PXCMTouchlessController.UXEventData data)
        {
            if (this.Dispatcher.CheckAccess())
            {
                switch (data.type)
                {
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_CursorVisible:
                        {
                            LabelStatus.Content = "Cursor Visible";
                            DisplayArea.Cursor = Cursors.Hand;
                        }
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_CursorNotVisible:
                        {
                            LabelStatus.Content = "Cursor Not Visible";
                            DisplayArea.Cursor = null;
                        }
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_Select:
                        {
                            LabelStatus.Content = "Select";
                            MouseInjection.ClickLeftMouseButton();
                        }
                        break;
                    //case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_StartScroll:
                    //    {
                    //        Console.WriteLine("Start Scroll");
                    //        initialScrollPoint = data.position.y;
                    //        initialScrollOffest = myListscrollViwer.VerticalOffset;
                    //    }
                    //    break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_CursorMove:
                        {
                            Point point = new Point();
                            point.X = Math.Max(Math.Min(1.0F, data.position.x), 0.0F);
                            point.Y = Math.Max(Math.Min(1.0F, data.position.y), 0.0F);

                            if (Math.Abs(oldXpos - data.position.x) < 0.005F && Math.Abs(oldYpos - data.position.y) < 0.005F) return;

                            Label11.Content = "data.position.x" + data.position.x;
                            Label2.Content = "data.position.y" + data.position.y;
                            oldXpos = data.position.x;
                            oldYpos = data.position.y;

                            Point myListBoxPosition = DisplayArea.PointToScreen(new Point(0.0, 0.0));
                            Point myListBoxPosition2 = DisplayArea.PointToScreen(new Point(DisplayArea.ActualWidth, DisplayArea.ActualHeight));
                            
                            var d1 = myListBoxPosition2.X - myListBoxPosition.X;
                            var d2 = myListBoxPosition2.Y - myListBoxPosition.Y;

                            int mouseX = (int)(myListBoxPosition.X + point.X * d1);
                            int mouseY = (int)(myListBoxPosition.Y + point.Y * d2);

                            Label3.Content = "mouseX" + mouseX;
                            Label4.Content = "mouseY" + mouseY;
                            MouseInjection.SetCursorPos(mouseX, mouseY);
                        }
                        break;
                    //case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_Scroll:
                    //    {
                    //        Console.WriteLine("Scrolling");
                    //        myListscrollViwer.ScrollToVerticalOffset(initialScrollOffest + (data.position.y - initialScrollPoint) * scrollSensitivity);
                    //    }
                    //    break;
                }
            }
            else
            {
                this.Dispatcher.Invoke(new Action(() => OnTouchlessControllerUXEvent(data)));
            }
        }

        private void UpdateConfiguration()
        {
            pxcmStatus rc;
            PXCMTouchlessController.ProfileInfo pInfo;

            rc = ptc.QueryProfile(out pInfo);
            Console.WriteLine("Querying Profile: " + rc.ToString());
            if (rc != pxcmStatus.PXCM_STATUS_NO_ERROR)
                Environment.Exit(-1);

            pInfo.config = PXCMTouchlessController.ProfileInfo.Configuration.Configuration_Scroll_Vertically;

            rc = ptc.SetProfile(pInfo);
            Console.WriteLine("Setting Profile: " + rc.ToString());
        }

        private void StartFrameLoop()
        {
            psm.StreamFrames(false);
        }

        private void Button_Click1(object sender, RoutedEventArgs e)
        {
            tButon2.IsChecked = false;
            tButon3.IsChecked = false;
        }

        private void Button_Click2(object sender, RoutedEventArgs e)
        {
            tButon1.IsChecked = false;
            tButon3.IsChecked = false;
        }

        private void Button_Click3(object sender, RoutedEventArgs e)
        {
            tButon2.IsChecked = false;
            tButon1.IsChecked = false;
        }
    }
}
