using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PictureReader
{
    public partial class MainForm : Form
    {
        private string src_pic_name;
        private readonly Label languageLabel;
        private readonly ComboBox languageSelector;

        public MainForm()
        {
            InitializeComponent();

            languageLabel = new Label
            {
                AutoSize = true,
                Location = new Point(474, 12),
                Name = "languageLabel"
            };
            languageSelector = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(536, 8),
                Name = "languageSelector",
                Size = new Size(131, 24),
                TabIndex = 22
            };
            languageSelector.Items.AddRange(new object[] { "English", "简体中文" });
            languageSelector.SelectedIndex = UiStrings.Culture.Name == "zh-CN" ? 1 : 0;
            languageSelector.SelectedIndexChanged += LanguageSelector_SelectedIndexChanged;
            Controls.Add(languageLabel);
            Controls.Add(languageSelector);
            ApplyLanguage();
        }

        private void LanguageSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            UiStrings.SetCulture(languageSelector.SelectedIndex == 1 ? "zh-CN" : "en-US");
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            Text = UiStrings.Get("WindowTitle");
            languageLabel.Text = UiStrings.Get("Language");
            btn_mult_convert.Text = UiStrings.Get("BatchConvert");
            btn_mult_convert.Width = 110;
            btn_single_convert.Text = UiStrings.Get("SingleConvert");
            btn_single_convert.Width = 110;
            btn_save.Text = UiStrings.Get("SaveFile");
            btn_save.Width = 85;
            label_src_pic_name.Text = string.IsNullOrEmpty(src_pic_name)
                ? UiStrings.Get("OriginalImage")
                : Path.GetFileName(src_pic_name);
            label2.Text = UiStrings.Get("ResizedImage");
            label3.Text = UiStrings.Get("ImageArray");
            tb_yuzhi.Text = UiStrings.Get("Threshold");
            cb_ascii.Text = UiStrings.Get("AsciiMode");
            cb_text.Text = UiStrings.Get("FontArrayMode");
            cb_reverse.Text = UiStrings.Get("Invert");
            label1.Text = UiStrings.Get("Opacity");
            ll_open_source_address.Text = UiStrings.Get("SourceCode");
        }

        

        private void Btn_mult_convert_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Title = UiStrings.Get("BatchConvert");
            openFileDialog.Filter = UiStrings.Get("ImageFilter");
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                
                string[] filenames = openFileDialog.FileNames;
                for (int i = 0; i < filenames.Length; i++)
                {
                    pb_src.Image = Image.FromFile(filenames[i]);
                    Bitmap bitmap = new Bitmap(pb_src.Image);
                    Bitmap newPic = convertSize(bitmap, 128, 64);
                    pb_to.Image = newPic;
                    src_pic_name = filenames[i];
                    newPic.Save(src_pic_name.Substring(0, src_pic_name.LastIndexOf(".")) + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);
                }
                MessageBox.Show(UiStrings.Get("BatchCompleted"), UiStrings.Get("Information"));

            }
        }

        private void Btn_single_convert_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = UiStrings.Get("SingleConvert");
            openFileDialog.Filter = UiStrings.Get("ImageFilter");
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var filename = openFileDialog.FileName;

                src_pic_name = filename;
                label_src_pic_name.Text = Path.GetFileName(filename);
                pb_src.Image = Image.FromFile(filename);
                convert();

            }
        }

        private Bitmap convertSize(Bitmap srcPicBitmap, int newWidth, int newHeight)
        {

            int width = srcPicBitmap.Width;
            int height = srcPicBitmap.Height;
            int xStart = width % newWidth/2;
            int yStart = height % newHeight / 2;
            int widthStep = width / newWidth;
            int heightStep = height / newHeight;
            Bitmap newPicBitmap = new Bitmap(newWidth, newHeight);
            int x = 0, y = 0;
            for (int i = xStart; i < width- xStart-1; i += widthStep)
            {
                for (int j = yStart; j < height-yStart-1; j += heightStep)
                {
                    Color color = srcPicBitmap.GetPixel(i, j);
                    if (cb_reverse.Checked == false)
                    {
                        if ((color.R + color.G + color.B) > Convert.ToInt32(nud_yuzhi.Value))
                        {
                            color = Color.Black;
                        }
                        else
                        {
                            color = Color.White;
                        }
                    }
                    else {
                        if ((color.R + color.G + color.B) < Convert.ToInt32(nud_yuzhi.Value))
                        {
                            color = Color.Black;
                        }
                        else
                        {
                            color = Color.White;
                        }
                    }
                    

                    newPicBitmap.SetPixel(x, y, color);
                    y++;
                }
                y = 0;
                x++;
            }
            convertToArray(newPicBitmap);
            return newPicBitmap;
        }





        private void nud_test(object sender, System.EventArgs e)
        {
            convert();
        }

        private void Btn_save_Click(object sender, EventArgs e)
        {
            Bitmap newPic = new Bitmap(pb_to.Image);
            newPic.Save(src_pic_name.Substring(0,src_pic_name.LastIndexOf("."))+".bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            MessageBox.Show(UiStrings.Get("SaveCompleted"), UiStrings.Get("Information"));
        }

        private void convertToArray(Bitmap bitmap,int mode)
        {
            int width = bitmap.Width;
            int high = bitmap.Height;
            string newString = "unsigned char yize_image[1024]={\r\n";
            int pixel = 0;
            Byte ch=0;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < high; j++) {
                    Color color = bitmap.GetPixel(i, j);
                    if ((color.R + color.G + color.B) < Convert.ToInt32(nud_yuzhi.Value))
                    {
                        pixel = 0;
                    }
                    else
                    {
                        pixel = 1;
                    }
                    if (j % 8 == 0)
                    {
                        newString+=ch+",";
                        ch = 0;
                    }
                    if (pixel == 1)
                    {
                        ch |= (Byte)(1 << (j % 8));
                    }
                }
                newString += "\r\n";
            }
            tb_data.Text=newString+"};";
        }

        private void convertToArray(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int high = bitmap.Height;
            string newString = "";
            int pixel = 0;
            Byte ch = 0;
            List<Byte> byteList = new List<byte>();
            if (cb_text.Checked)
            {
                newString += "unsigned char yize_image[1024]={\r\n";
            }
            for (int page = 0; page < 8; page++)
            {
                for (int i = 0; i < 128; i++)
                {
                    for (int j = page * 8; j < page * 8 + 8; j++)
                    {
                        Color color = bitmap.GetPixel(i, j);
                        if ((color.R + color.G + color.B) < Convert.ToInt32(nud_yuzhi.Value))
                        {
                            pixel = 0;
                        }
                        else
                        {
                            pixel = 1;
                        }
                        if (j % 8 == 0)
                        {
                            //byteList.Add(ch);
                           
                            if (cb_ascii.Checked)
                            {
                                newString += ch.ToString("X2") + " ";
                            }else if(cb_text.Checked){
                                newString += ch + ",";
                            }
                            
                            ch = 0;
                        }
                        if (pixel == 1)
                        {
                            ch |= (Byte)(1 << (j % 8));
                        }
                    }  
                }
                if (cb_text.Checked)
                {
                    newString += "\r\n";
                }
            }
            //byteList.ToString();
            if (cb_text.Checked)
            {
                newString += "};";
            }
            tb_data.Text = newString ;
            
        }

        private void Cb_text_CheckedChanged(object sender, EventArgs e)
        {
            if (cb_ascii.Checked && cb_text.Checked)
            {
                cb_ascii.Checked = false;
            }
            else {
                cb_ascii.Checked = true;
            }

            convert();
        }

        private void Cb_ascii_CheckedChanged(object sender, EventArgs e)
        {
            if (cb_ascii.Checked && cb_text.Checked)
            {
                cb_text.Checked = false;
            }
            else
            {
                cb_text.Checked = true;
            }
            if (pb_src.Image == null)
            {
                MessageBox.Show(UiStrings.Get("SelectImageFirst"), UiStrings.Get("Information"));
                return;
            }
            convert();
        }

        private void Cb_reverse_CheckedChanged(object sender, EventArgs e)
        {
            convert();
        }

        private void convert()
        {
            if (pb_src.Image == null)
            {
                MessageBox.Show(UiStrings.Get("SelectImageFirst"), UiStrings.Get("Information"));
                return;
            }
            Bitmap bitmap = new Bitmap(pb_src.Image);
            Bitmap newPic = convertSize(bitmap, 128, 64);
            pb_to.Image = newPic;
        }

        private void Track_bar_ocap_ValueChanged(object sender, EventArgs e)
        {
            Opacity = track_bar_ocap.Value / 100.0;
        }

        private void Ll_open_source_address_Clicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/bestyize/PictureFetcher") { UseShellExecute = true });
        }
    }
}
