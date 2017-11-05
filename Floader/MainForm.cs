﻿using System;
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
            
        }
        List<string> _downloads = new List<string>();
        List<string> _failedDownloads = new List<string>();
        private List<string> _thumbnails = new List<string>();
        private ImageList _thumbs;
        private string _thumbnailSavePath;
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
                await DownloadPictures(_downloads);
                if (_failedDownloads.Count <= 0) return;
                var report = _failedDownloads.ToList();

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
            int i = 0, var = _downloads.Count;
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
                        _failedDownloads.Add(url);
                    }
                }
                DloadProgressLabel.Text = @"Finished";
                progressBar.Value = 0;
            }
        }
        private static bool EvaluateUrl(string url)
        {
            var m = Regex.Match(url, @"^(http|www)");
            if (!m.Success) return false;
            //try
            //{
            //    MessageBox.Show("Entered into EvaluateUrl before request!");
            //    var request = (HttpWebRequest)WebRequest.Create(url);
            //    request.UserAgent = UserAgent;
            //    var response = (HttpWebResponse)request.GetResponse();
            //    if (response.StatusCode != HttpStatusCode.OK) return false;
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.Message);
            //    throw;
            //}
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
                var links = LoadLinks(textBoxLink.Text);
                _thumbnails = links.Item1; //Populating _downloads and thumbnails
                _downloads = links.Item2;
                _thumbs = fetchThumbnails(_thumbnails);

                listViewLinks.LargeImageList = _thumbs;
                listViewLinks.CheckBoxes = true;

                //foreach (var a in _downloads)
                //{
                //    _imageIndex++;

                //    var lvi = new ListViewItem();
                //    lvi.Text = _imageIndex.ToString();
                //    lvi.SubItems.Add(a);
                    
                //    lvi.SubItems.Add(_imageIndex.ToString());
                //    listViewLinks.Items.Add(lvi);
                //    //listViewLinks.Items.Add(a,);
                //}
                InfoLbl.Text = "";
            }
            InfoLbl.Text = @"Please select a profile!";
        }
        private void btnReset_Click(object sender, EventArgs e)
        {
            _imageIndex = 0;
            _secondPattern = null;
            _firstPattern = null;
            _subLink = null;
            _secondSubLink = null;
            _secondStage = false;
            _downloads.Clear();
            _failedDownloads.Clear();
            InfoLbl.Text = null;
            comboBoxRegEx.Text = null;
            listViewLinks.Items.Clear();
        }
        private void comboBoxRegEx_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            switch (comboBoxRegEx.Text)
            {
                case "www.af.mil":
                    textBoxLink.Text = @"http://www.af.mil/News/Photos.aspx?igcategory=Aircraft";
                    _thumbPattern = @"https:\/\/media.defense.gov+\/.*.\/.*.\/.*.\/.*.\/213\/.*\/*.JPG"; //TODO
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
        private Tuple<List<string>,List<string>> LoadLinks(string url)
        {
            var thumbnails = new List<string>();
            var images = new List<string>();
            var tuple = new Tuple<List<string>,List<string>>(thumbnails,images);
            if (_secondStage == false)  //Checks if second page to image is true
            {
                //MessageBox.Show("Entered into secondStage = false"); //debug code
                var data = GetHtml(url);
                foreach (var s in data)
                {
                    try
                    {
                        var matchThumb = Regex.Match(s, _thumbPattern);
                        if (!matchThumb.Success) continue;
                        var thumbName = matchThumb.Value;
                        if (!_thumbnails.Contains(_subLink + thumbName + _secondSubLink))
                            _thumbnails.Add(_subLink + thumbName + _secondSubLink);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }
                    try
                    {
                        var match = Regex.Match(s, _firstPattern);
                        if (!match.Success) continue;
                        var name = match.Value;
                        if (!images.Contains(_subLink + name + _secondSubLink))
                            images.Add(_subLink + name + _secondSubLink);
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }
                }
                return tuple;
            }
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
            return tuple;
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
                MessageBox.Show(e.Message);
            }
            return data;   
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listViewLinks.LabelEdit = true;

            listViewLinks.AllowColumnReorder = true;

            listViewLinks.CheckBoxes = true;

            listViewLinks.FullRowSelect = true;

            listViewLinks.GridLines = true;
            listViewLinks.Columns.Add("#");
            listViewLinks.Columns.Add("Name");

            ImageList il = new ImageList();
            il.Images.Add("test1", Image.FromFile(@"C:\Users\Crow\Desktop\test\1.jpg"));
            il.Images.Add("test2", Image.FromFile(@"C:\Users\Crow\Desktop\test\2.jpg"));

            listViewLinks.View = View.LargeIcon;

            il.ImageSize = new Size(120,120);
            listViewLinks.LargeImageList = il;
            listViewLinks.Items.Add("test");

            //for (var i = 0; i < il.Images.Count; i++)
            //{
            //    ListViewItem lvi = new ListViewItem();
            //    lvi.ImageIndex = i;
            //    lvi.Text = i.ToString();
            //    listViewLinks.Items.Add(lvi);
            //}
        }

        private static ImageList fetchThumbnails(List<string> thumbUrlList)
        {
            ImageList thumbList = new ImageList();
            using (var webClient = new WebClient())
            {
                foreach (var thumb in thumbUrlList)
                {
                    var bitmapData = webClient.DownloadData(thumb);

                    // Bitmap data => bitmap => resized bitmap.            
                    using (MemoryStream memoryStream = new MemoryStream(bitmapData))
                    using (Bitmap bitmap = new Bitmap(memoryStream))
                    using (Bitmap resizedBitmap = new Bitmap(bitmap, 50, 50))
                    {
                        // NOTE:
                        // Resized bitmap must be disposed because the imageList.Images.Add() method
                        // makes a copy (!) of the source bitmap!
                        // For details, see https://stackoverflow.com/questions/9515759/                
                        thumbList.Images.Add(thumb.Substring(thumb.LastIndexOf('/')),resizedBitmap);
                    }
                }
            }
            return thumbList;
        }
    }
}