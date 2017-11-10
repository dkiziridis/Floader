using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace Floader
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            listViewLinks.LabelEdit = true;
            listViewLinks.AllowColumnReorder = false;
            listViewLinks.CheckBoxes = true;
            listViewLinks.FullRowSelect = true;
            listViewLinks.GridLines = true;
            listViewLinks.Columns.Add("Images");
            listViewLinks.View = View.Details;
            btnSelectAll.Visible = false;
            btnSelectNone.Visible = false;
            _thumbnails.ImageSize = new Size(120, 80);
            _thumbnails.ColorDepth = ColorDepth.Depth24Bit;
            listViewLinks.SmallImageList = _thumbnails;
           
        }
        List<string> _downloadLinks = new List<string>();
        List<string> _failedDownloadLinks = new List<string>();
        private List<string> _thumbnailLinks = new List<string>();
        private List<string> _imageLinks = new List<string>();
        private ImageList _thumbnails = new ImageList();
        private string _savePath;
        private string _secondPattern;
        private string _thumbPattern;
        private string _firstPattern;
        private string _subLink;
        private string _secondSubLink;
        private bool? _secondStage;
        private string _linkInput;
        private int _imageIndex = 0;
        private const string UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";

        private async void btnDownload_Click(object sender, EventArgs e)
        {
            if (_savePath == null)
            {
                btnSaveTo_Click(sender, e);
            }
            if (_savePath != null)
            {
                foreach(ListViewItem imgLink in listViewLinks.CheckedItems)
                {
                    if(!_downloadLinks.Contains(imgLink.Text))
                        _downloadLinks.Add(imgLink.Text);
                }
                await DownloadPictures(_downloadLinks);

                if (_failedDownloadLinks.Count <= 0) return;
                var report = _failedDownloadLinks.ToList();

                var s = string.Join(" ", report);
                MessageBox.Show(s, @"Failed Downloads");
            }
            else
            {
                InfoLbl.Text = @"Please select a folder.";
            }
        }

        private async Task DownloadPictures(IEnumerable<string> imageList)// Downloads images found in _downloads list
        {

            progressBar.Value = 0;
            int i = 0, var = _downloadLinks.Count;
            DloadProgressLabel.Visible = true;
            using (var webClient = new WebClient())
            {
                foreach (var url in imageList)
                {
                    i++;
                    try
                    {
                        if (!File.Exists(_savePath + @"\" + url.Substring(url.LastIndexOf('/'))))
                        {
                            DloadProgressLabel.Text = @"Downloading " + i + @"/" + var;
                            webClient.Headers.Add("user-agent", UserAgent);
                            await webClient.DownloadFileTaskAsync(new Uri(url), _savePath + @"\" + url.Substring(url.LastIndexOf('/')));
                            progressBar.Value += 100 / var;
                        }
                    }
                    catch (WebException)
                    {
                        File.Delete(_savePath+@"\"+url.Substring(url.LastIndexOf('/')));
                        _failedDownloadLinks.Add(url);
                    }
                }
                DloadProgressLabel.Text = @"Finished";
                progressBar.Value = 0;
            }
        }

        private static bool EvaluateUrl(string url)
        {
            bool isUri = Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute);
            if (!isUri) return false;
            // var m = Regex.Match(url, @"^(http|www)");
            //if (!m.Success) return false;
            HttpWebRequest request;
            HttpWebResponse response;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = UserAgent;
                response = (HttpWebResponse)request.GetResponse();
            }
            catch(Exception)
            {
                return false;
            }
            return true;
        } //Check if txtBox link is valid

        private void btnSaveTo_Click(object sender, EventArgs e)
        {
            var folderDlg = new FolderBrowserDialog {ShowNewFolderButton = true};
            var result = folderDlg.ShowDialog();
            if (result != DialogResult.OK) return;
            _savePath = folderDlg.SelectedPath;
            labelSaveTo.Text = @"Saving to : " + folderDlg.SelectedPath;
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            if (!EvaluateUrl(textBoxLink.Text))
            {
                InfoLbl.Text = @"Invalid Link Entered!";
                return;
            }
            if (_secondStage.HasValue)
            {
                try
                {
                    _linkInput = new Uri(textBoxLink.Text).Host;
                    var combo = comboBoxRegEx.Text;
                    if (_linkInput != combo)
                    {
                        InfoLbl.Text = @"Profile doesn't match URL!";
                        return;
                    }
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.Message);
                }
                InfoLbl.Text = @"Working...";
                //save links to tuple

                Tuple<List<string>, List<String>> thumbsAndImagesTuple;
                thumbsAndImagesTuple = FetchLinks(textBoxLink.Text);
                _thumbnailLinks.Clear(); //Temp var
                _thumbnailLinks.AddRange(thumbsAndImagesTuple.Item1);
                _imageLinks.AddRange(thumbsAndImagesTuple.Item2);
                
                using (var webClient = new WebClient())
                {
                    foreach (var thumb in _thumbnailLinks)
                    {
                        try
                        {
                            webClient.Headers.Add("user-agent", UserAgent);
                            byte[] data = webClient.DownloadData(thumb);
                            using (MemoryStream imageData = new MemoryStream(data))
                            {
                                Image img = Image.FromStream(imageData);
                                _thumbnails.Images.Add(img);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message,"Thumbnail retrieval error!");
                            btnReset_Click(sender, e); //Reset app status in case of failure i.e. WebException
                            return;
                        }
                    }
                }
                
                int i = _imageIndex;
                for (i = _imageIndex; i < _thumbnails.Images.Count; i++)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.ImageIndex = i;
                    lvi.Text = _imageLinks.ElementAt(i);
                    listViewLinks.Items.Add(lvi);
                    _imageIndex = i;
                }
                listViewLinks.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                if(listViewLinks.Items.Count > 0)
                {
                    btnSelectNone.Visible = true;
                    btnSelectAll.Visible = true;
                }
                InfoLbl.Text = "";
            }
            else
            {
                InfoLbl.Text = @"Please select a profile!";
            }
           
        }
        private void btnReset_Click(object sender, EventArgs e)
        {
            _imageLinks.Clear();
            _thumbnails.Images.Clear();
            _secondPattern = null;
            _imageIndex = 0;
            _firstPattern = null;
            _subLink = null;
            _secondSubLink = null;
            _secondStage = false;
            _downloadLinks.Clear();
            _failedDownloadLinks.Clear();
            InfoLbl.Text = null;
            comboBoxRegEx.Text = null;
            listViewLinks.Items.Clear();
            btnSelectAll.Visible = false;
            btnSelectNone.Visible = false;
        }
        private void comboBoxRegEx_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            switch (comboBoxRegEx.Text)
            {
                //case "Generic":
                //    textBoxLink.Text = "";
                //    _thumbPattern = @"https:\/\/media.defense.gov+\/.*.\/.*.\/.*.\/.*.\/213\/.*\/*.JPG";
                //    _subLink = null;
                //    _secondSubLink = null;
                //    _firstPattern = @"https://media.defense.gov/.*./.*./.*./.*./-1.*/*.JPG";
                //    _secondPattern = null;
                //    _secondStage = false;
                //    break;
                case "www.af.mil":
                    textBoxLink.Text = @"http://www.af.mil/News/Photos.aspx?igcategory=Aircraft";
                    _thumbPattern = @"https:\/\/media.defense.gov+\/.*.\/.*.\/.*.\/.*.\/213\/.*\/*.JPG"; 
                    _subLink = null;
                    _secondSubLink = null;
                    _firstPattern = @"https://media.defense.gov/.*./.*./.*./.*./-1.*/*.JPG";
                    _secondPattern = null;
                    _secondStage = false;
                    break;
                case "www.pexels.com":
                    textBoxLink.Text = @"https://www.pexels.com/";
                    _thumbPattern = @""; //TODO
                    _subLink = null;
                    _secondSubLink = null;
                    _firstPattern = @"(https:\/\/(images|static)\.pexels\.com\/photos\/)+(\d*)\/pexels-photo-+(\d*)\.\w*";
                    _secondPattern = null;
                    _secondStage = false;
                    break;
                case "alpha.wallhaven.cc":
                    textBoxLink.Text = @"https://alpha.wallhaven.cc/";
                    _subLink = @"https://wallpapers.wallhaven.cc/wallpapers/full/wallhaven";
                    _thumbPattern = @""; //TODO
                    _secondSubLink = null;
                    _firstPattern = @"-(\d*)+\.(png|jpg)";
                    _secondPattern = null;
                    _secondStage = false;
                    break;
            }
        }
        private Tuple<List<string>,List<string>> FetchLinks(string url)
        {
            var thumbnails = new List<string>();
            var images = new List<string>();
            var tuple = new Tuple<List<string>,List<string>>(thumbnails,images);
            if (_secondStage == false)  //Checks if second page to image is true
            {
                var data = GetHtml(url);
                foreach (var s in data)
                {
                    try
                    {
                        var matchThumb = Regex.Match(s, _thumbPattern);
                        if (matchThumb.Success)
                        {
                            var thumbName = matchThumb.Value;
                            if (!thumbnails.Contains(_subLink + thumbName + _secondSubLink))
                                thumbnails.Add(_subLink + thumbName + _secondSubLink);
                        }
                       
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }
                    try
                    {
                        var match = Regex.Match(s, _firstPattern);
                        if (match.Success)
                        {
                            var name = match.Value;
                            if (!images.Contains(_subLink + name + _secondSubLink))
                                images.Add(_subLink + name + _secondSubLink);
                        }
                        
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }
                }
                //returns links to thumbnails and full size images
            }
            return tuple;
            //{
            //    // MessageBox.Show("Entered into secondStage = true"); //debug code
            //    var urlList = new List<string>();
            //    var pseudoLinks = GetHtml(url);
            //    foreach (var a in pseudoLinks)
            //    {
            //        var match = Regex.Match(a, _firstPattern);
            //        if (!match.Success) continue;
            //        var name = match.Value;
            //        if (!urlList.Contains(_subLink + name)) //love you long time
            //            urlList.Add(_subLink + name);
            //    }
            //    foreach (var s in urlList)
            //    {
            //        var newlinks = GetHtml(s);
            //        foreach (var v in newlinks)
            //        {
            //            var match = Regex.Match(v, _secondPattern);
            //            if (!match.Success) continue;
            //            if (!images.Contains(_secondSubLink + match.Value))
            //                images.Add(_secondSubLink + match.Value);
            //        }
            //    }
            //    return tuple;
            //}
            //return tuple;
        }
        private static IEnumerable<string> GetHtml(string urlAddress)
        {
            var data = new List<string>();
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(urlAddress);
                request.UserAgent = UserAgent;
                var response = (HttpWebResponse)request.GetResponse();
                var receiveStream = response.GetResponseStream();
                Debug.Assert(receiveStream != null, "receiveStream != null");
                var readStream = response.CharacterSet == null ? new StreamReader(receiveStream) : new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                string line;
                while ((line = readStream.ReadLine()) != null)
                {
                    data.Add(line);
                }
                if(data.Count  <= 3)    //runs in case html code is in single line
                {
                    var joinedList = string.Join(", ", data.ToArray());
                    var multiLineHtml = joinedList.Split('>').ToList();
                    response.Close();
                    readStream.Close();
                    return multiLineHtml;
                }
                response.Close();
                readStream.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Cannot obtain link source!");
            }
            return data;   
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem cn in listViewLinks.Items)
            {
                if (!cn.Checked)
                {
                    cn.Checked = true;
                }
            }
            listViewLinks.Refresh();
        }

        private void btnSelectNone_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem cn in listViewLinks.Items)
            {
                if (cn.Checked)
                {
                    cn.Checked = false;
                }
            }
            listViewLinks.Refresh();
        }
    }
}