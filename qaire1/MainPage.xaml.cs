using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace qaire1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //record all the events to this. A background thread will be constantly saving what happens.
        public static ConcurrentQueue<string> eventRecord = new ConcurrentQueue<string>();

        public MainPage()
        {
            this.InitializeComponent();
            init();

            //schedule a consumer for the thread pool that goes every second.
            //We may miss an event at the end.
            ThreadPoolTimer timer1 =
                ThreadPoolTimer.CreatePeriodicTimer((source) => { consumeRecord(); },
                TimeSpan.FromSeconds(1));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Remove the UI from the title bar if there are no pages in our in-app back stack
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }

        List<Object> mObjects;
        public async void init()
        {
            var localRoot = ApplicationData.Current.LocalFolder.Path;
            await parseXML();

            mObjects = new List<Object>();

            int count = 0;
            foreach (var q in mQuestions)
            {
                var lbl = new Button();
                lbl.Tag = q;
                lbl.Content = q.title;

                lbl.Click += Lbl_Click;

                mObjects.Add(lbl);
                spSelect.Children.Add(lbl);

                if(count==0)
                {
                    myframe.Navigate(typeof(QuestionPage1), lbl.Tag);
                    lbl.Background = new SolidColorBrush(Colors.Gray);
                }

                ++count;
            }
        }

        private void Lbl_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            //rootFrame.Navigate(typeof(QuestionPage1), (sender as Button).Tag);
            myframe.Navigate(typeof(QuestionPage1), (sender as Button).Tag); //to switch to next version (way better)
            (sender as Button).Background = new SolidColorBrush(Colors.Gray);
        }


        //consume any events on the record
        private async void consumeRecord()
        {
            if (eventRecord != null && eventRecord.Count > 0)
            {
                string fname = "qaire\\questionnaire_log_" +
                    DateTime.Now.Day.ToString() + "_" +
                    DateTime.Now.Month.ToString() + "_" +
                    DateTime.Now.Year.ToString() + "_" +
                    DateTime.Now.Hour.ToString() + ".dat";
                var fil = await KnownFolders.DocumentsLibrary.CreateFileAsync(fname,
                    CreationCollisionOption.OpenIfExists);
                string dat;
                string dump = "";
                while(eventRecord.TryDequeue(out dat))
                {
                    dump += dat;
                    dump += "\n";
                }

                if (dump.Length > 0)
                {
                    await FileIO.AppendTextAsync(fil, dump);
                }
            }
        }

        public static async Task<Windows.Data.Xml.Dom.XmlDocument> LoadXmlFile(String folder, String file)
        {
            StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFile storageFile = await storageFolder.GetFileAsync(file);
            Windows.Data.Xml.Dom.XmlLoadSettings loadSettings = new Windows.Data.Xml.Dom.XmlLoadSettings();
            loadSettings.ProhibitDtd = false;
            loadSettings.ResolveExternals = false;
            return await Windows.Data.Xml.Dom.XmlDocument.LoadFromFileAsync(storageFile, loadSettings);
        }
        
        //Read in the XML file that contains the questions.
        List<questionStructure> mQuestions;
        private async Task parseXML()
        {
            mQuestions = new List<questionStructure>();
            Windows.Data.Xml.Dom.XmlDocument doc = await LoadXmlFile("", "questions.xml");

            txtSummary.Text = doc.GetElementsByTagName("introText").ElementAt(0).Attributes.GetNamedItem("statement").NodeValue.ToString();

            foreach (var nd in doc.GetElementsByTagName("page"))
            {
                var q = new questionStructure();
                q.title = nd.Attributes.GetNamedItem("title").NodeValue.ToString();

                foreach(var subnd in nd.ChildNodes)
                {
                    if(subnd.NodeName == "header")
                    {
                        q.header = subnd.Attributes.GetNamedItem("statement").NodeValue.ToString();
                    }
                    else if (subnd.NodeName == "shortname")
                    {
                        q.shortName = subnd.Attributes.GetNamedItem("statement").NodeValue.ToString();
                    }
                    else if (subnd.NodeName == "footer")
                    {
                        q.footer = subnd.Attributes.GetNamedItem("statement").NodeValue.ToString();
                    }
                    else if (subnd.NodeName == "checkbox")
                    {
                        AddCheckBox(ref q, subnd);
                    }
                    else if (subnd.NodeName == "writebox")
                    {
                        AddWriteBox(ref q, subnd);
                    }
                    else if (subnd.NodeName == "likert")
                    {
                        AddLikertScale(ref q, subnd);
                    }
                    else if (subnd.NodeName == "image_block")
                    {
                        AddImageTile(ref q, subnd);
                    }
                }
                
                mQuestions.Add(q);
            }
        }

        private void AddImageTile(ref questionStructure q, IXmlNode subnd)
        {
            var t = new TextBlock();
            t.TextWrapping = TextWrapping.WrapWholeWords;
            t.Text = subnd.Attributes.GetNamedItem("statement").NodeValue.ToString();
            q.body.Add(t);

            var c = new Canvas();
            c.Width = Convert.ToDouble(subnd.Attributes.GetNamedItem("width").NodeValue.ToString());
            c.Height = Convert.ToDouble(subnd.Attributes.GetNamedItem("height").NodeValue.ToString());
            ImageBrush buttonBR = new ImageBrush();
            buttonBR.ImageSource = new BitmapImage(new Uri(this.BaseUri, "/Assets/"+ subnd.Attributes.GetNamedItem("image_file").NodeValue.ToString()));
            buttonBR.Stretch = Stretch.Fill;
            c.Background = buttonBR;

            q.body.Add(c);
        }

        //add a likert scale (as in "How much do you like pizza on a scale of 1-5?")
        //<likert title = "" name="gal_1" number="5" left_title="Don't remember" right_title="Remember most certainly", tag="pg7_lk1"/>
        private void AddLikertScale(ref questionStructure q, IXmlNode subnd)
        {
            int size = 70; //could be a parameter

            var gr = new Grid();
            gr.Tag = "likert";
            RowDefinition row1 = new RowDefinition();
            gr.RowDefinitions.Add(row1);
            RowDefinition row2 = new RowDefinition();
            row2.Height = new GridLength(size);
            gr.RowDefinitions.Add(row2);

            //room for the left label
            var cl = new ColumnDefinition();
            //cl.Width = GridLength.Auto;
            gr.ColumnDefinitions.Add(cl);

            int numEntries = Convert.ToInt32(subnd.Attributes.GetNamedItem("number").NodeValue.ToString());
            for (int i = 0; i < numEntries; ++i)
            {
                var c = new ColumnDefinition();
                c.Width = new GridLength(size);
                gr.ColumnDefinitions.Add(c);
            }

            //room for the right label
            var cr = new ColumnDefinition();
            //cr.Width = GridLength.Auto;
            gr.ColumnDefinitions.Add(cr);

            //label at the top
            var instructions = new TextBlock();
            instructions.Text = subnd.Attributes.GetNamedItem("statement").NodeValue.ToString();
            Grid.SetRow(instructions, 0);
            Grid.SetColumn(instructions, 0);
            Grid.SetColumnSpan(instructions, numEntries+2);
            instructions.TextWrapping = TextWrapping.WrapWholeWords;
            gr.Children.Add(instructions);

            var left = new TextBlock();
            Grid.SetRow(left, 1);
            Grid.SetColumn(left, 0);
            left.TextWrapping = TextWrapping.WrapWholeWords;
            left.TextAlignment = TextAlignment.Center;
            left.VerticalAlignment = VerticalAlignment.Center;
            left.Text = subnd.Attributes.GetNamedItem("left_title").NodeValue.ToString() + "   ";
            gr.Children.Add(left);

            for (int i = 1; i <= numEntries; ++i) 
            {
                var cb = new RadioButton();
                Grid.SetRow(cb, 1);
                Grid.SetColumn(cb, i);
                //cb.HorizontalAlignment = HorizontalAlignment.Right;
                cb.Content = i.ToString();
                gr.Children.Add(cb);
                cb.Tag = subnd.Attributes.GetNamedItem("tag").NodeValue.ToString() + "_" + i.ToString();
                //q.cbData[cb] = "unchecked";
                eventRecord.Enqueue(String.Format("{0} {1}",
                    HighResolutionDateTime.now.ToString(),
                    cb.Tag + "_created"));
            }

            var right = new TextBlock();
            right.TextWrapping = TextWrapping.WrapWholeWords;
            right.TextAlignment = TextAlignment.Left;
            right.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetRow(right, 1);
            Grid.SetColumn(right, numEntries+1);
            right.Text = "  " + subnd.Attributes.GetNamedItem("right_title").NodeValue.ToString();
            gr.Children.Add(right);

            q.body.Add(gr);
        }

        //adds both the writeable area and a button to extend the writeable area
        private void AddWriteBox(ref questionStructure q, IXmlNode subnd)
        {
            //var lv = new ListView();
            //lv.Header = subnd.Attributes.GetNamedItem("statement").NodeValue.ToString();

            var freeResponse = new TextBlock();
            freeResponse.TextWrapping = TextWrapping.Wrap;
            freeResponse.Text = subnd.Attributes.GetNamedItem("statement").NodeValue.ToString();
            q.body.Add(freeResponse);

            //TODO: make this stretchable
            var gr = new Grid();
            //gr.Height = Convert.ToDouble(subnd.Attributes.GetNamedItem("height").NodeValue.ToString());
            gr.Tag = subnd.Attributes.GetNamedItem("tag").NodeValue.ToString() + "_grid";
            gr.Background = new SolidColorBrush(Windows.UI.Colors.LightSlateGray);
            gr.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Black);
            RowDefinition row1 = new RowDefinition();
            row1.Height = new GridLength(Convert.ToDouble(subnd.Attributes.GetNamedItem("height").NodeValue.ToString()));
            gr.RowDefinitions.Add(row1);

            var ink = new InkCanvas();
            ink.Tag = subnd.Attributes.GetNamedItem("tag").NodeValue.ToString();

            eventRecord.Enqueue(String.Format("{0} {1}",
                HighResolutionDateTime.now.ToString(),
                "ink_" + subnd.Attributes.GetNamedItem("tag").NodeValue.ToString() + "_created"));

            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
            drawingAttributes.Color = Windows.UI.Colors.Blue;
            drawingAttributes.Size = new Size(2, 2);
            drawingAttributes.IgnorePressure = false;
            drawingAttributes.FitToCurve = true;

            ink.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
            ink.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;
            //ink.

            gr.Children.Add(ink);
            Grid.SetRow(ink, 0);

            //allow extending the writing region
            var extendBT = new Button();
            extendBT.Height = 30;
            extendBT.Width = 100;
            ImageBrush buttonBR = new ImageBrush();
            buttonBR.ImageSource = new BitmapImage(new Uri(this.BaseUri, "/Assets/down_arrow.png"));
            buttonBR.Stretch = Stretch.None;
            extendBT.Background = buttonBR;
            Grid.SetRow(extendBT, 0);
            extendBT.HorizontalAlignment = HorizontalAlignment.Right;
            extendBT.VerticalAlignment = VerticalAlignment.Bottom;

            extendBT.DataContext = gr;

            gr.Children.Add(extendBT);

            q.body.Add(gr);

        }

        //parse the checkbox object
        private void AddCheckBox(ref questionStructure q, IXmlNode subnd)
        {
            var lv = new ListView();
            lv.Header = subnd.Attributes.GetNamedItem("title").NodeValue.ToString();

            if (subnd.Attributes.GetNamedItem("single").NodeValue.ToString() == "True")
            {
                lv.SelectionMode = ListViewSelectionMode.Single;
            }

            foreach (var entry in subnd.ChildNodes)
            {
                if (entry.NodeName == "entry" && subnd.Attributes.GetNamedItem("single").NodeValue.ToString() == "True")
                {
                    var cb = new RadioButton();
                    //cb.Height = Convert.ToDouble(entry.Attributes.GetNamedItem("height"));
                    cb.Content = entry.Attributes.GetNamedItem("statement").NodeValue.ToString();
                    lv.Items.Add(cb);
                    cb.Tag = entry.Attributes.GetNamedItem("tag").NodeValue.ToString();
                    //q.cbData[cb] = "unchecked";
                    eventRecord.Enqueue(String.Format("{0} {1}",
                        HighResolutionDateTime.now.ToString(),
                        "rb_" + entry.Attributes.GetNamedItem("tag").NodeValue.ToString() + "_created"));
                }
                else if (entry.NodeName == "entry" && subnd.Attributes.GetNamedItem("single").NodeValue.ToString() != "True")
                {
                    var cb = new CheckBox();
                    //cb.Height = Convert.ToDouble(entry.Attributes.GetNamedItem("height"));
                    cb.Content = entry.Attributes.GetNamedItem("statement").NodeValue.ToString();
                    lv.Items.Add(cb);
                    cb.Tag = entry.Attributes.GetNamedItem("tag").NodeValue.ToString();
                    q.cbData[cb] = "unchecked";
                    eventRecord.Enqueue(String.Format("{0} {1}",
                        HighResolutionDateTime.now.ToString(),
                        "cb_" + entry.Attributes.GetNamedItem("tag").NodeValue.ToString() + "_created"));
                }
            }
            q.body.Add(lv);
        }
    }
}
