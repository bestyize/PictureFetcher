namespace PictureReader
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.btn_mult_convert = new System.Windows.Forms.Button();
            this.label_src_pic_name = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.pb_src = new System.Windows.Forms.PictureBox();
            this.pb_to = new System.Windows.Forms.PictureBox();
            this.tb_yuzhi = new System.Windows.Forms.Label();
            this.nud_yuzhi = new System.Windows.Forms.NumericUpDown();
            this.btn_save = new System.Windows.Forms.Button();
            this.tb_data = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cb_ascii = new System.Windows.Forms.CheckBox();
            this.cb_text = new System.Windows.Forms.CheckBox();
            this.cb_reverse = new System.Windows.Forms.CheckBox();
            this.btn_single_convert = new System.Windows.Forms.Button();
            this.track_bar_ocap = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pb_src)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_to)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_yuzhi)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.track_bar_ocap)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_mult_convert
            // 
            resources.ApplyResources(this.btn_mult_convert, "btn_mult_convert");
            this.btn_mult_convert.Name = "btn_mult_convert";
            this.btn_mult_convert.UseVisualStyleBackColor = true;
            this.btn_mult_convert.Click += new System.EventHandler(this.Btn_mult_convert_Click);
            // 
            // label_src_pic_name
            // 
            resources.ApplyResources(this.label_src_pic_name, "label_src_pic_name");
            this.label_src_pic_name.Name = "label_src_pic_name";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // pb_src
            // 
            this.pb_src.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.pb_src.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.pb_src, "pb_src");
            this.pb_src.Name = "pb_src";
            this.pb_src.TabStop = false;
            // 
            // pb_to
            // 
            this.pb_to.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.pb_to.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            resources.ApplyResources(this.pb_to, "pb_to");
            this.pb_to.Name = "pb_to";
            this.pb_to.TabStop = false;
            // 
            // tb_yuzhi
            // 
            resources.ApplyResources(this.tb_yuzhi, "tb_yuzhi");
            this.tb_yuzhi.Name = "tb_yuzhi";
            // 
            // nud_yuzhi
            // 
            this.nud_yuzhi.Increment = new decimal(new int[] {
            3,
            0,
            0,
            0});
            resources.ApplyResources(this.nud_yuzhi, "nud_yuzhi");
            this.nud_yuzhi.Maximum = new decimal(new int[] {
            765,
            0,
            0,
            0});
            this.nud_yuzhi.Name = "nud_yuzhi";
            this.nud_yuzhi.Value = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.nud_yuzhi.ValueChanged += new System.EventHandler(this.nud_test);
            // 
            // btn_save
            // 
            resources.ApplyResources(this.btn_save, "btn_save");
            this.btn_save.Name = "btn_save";
            this.btn_save.UseVisualStyleBackColor = true;
            this.btn_save.Click += new System.EventHandler(this.Btn_save_Click);
            // 
            // tb_data
            // 
            this.tb_data.BackColor = System.Drawing.SystemColors.Window;
            this.tb_data.ForeColor = System.Drawing.SystemColors.WindowFrame;
            resources.ApplyResources(this.tb_data, "tb_data");
            this.tb_data.Name = "tb_data";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // cb_ascii
            // 
            resources.ApplyResources(this.cb_ascii, "cb_ascii");
            this.cb_ascii.Checked = true;
            this.cb_ascii.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cb_ascii.Name = "cb_ascii";
            this.cb_ascii.UseVisualStyleBackColor = true;
            this.cb_ascii.CheckedChanged += new System.EventHandler(this.Cb_ascii_CheckedChanged);
            // 
            // cb_text
            // 
            resources.ApplyResources(this.cb_text, "cb_text");
            this.cb_text.Name = "cb_text";
            this.cb_text.UseVisualStyleBackColor = true;
            this.cb_text.CheckedChanged += new System.EventHandler(this.Cb_text_CheckedChanged);
            // 
            // cb_reverse
            // 
            resources.ApplyResources(this.cb_reverse, "cb_reverse");
            this.cb_reverse.Name = "cb_reverse";
            this.cb_reverse.UseVisualStyleBackColor = true;
            this.cb_reverse.CheckedChanged += new System.EventHandler(this.Cb_reverse_CheckedChanged);
            // 
            // btn_single_convert
            // 
            resources.ApplyResources(this.btn_single_convert, "btn_single_convert");
            this.btn_single_convert.Name = "btn_single_convert";
            this.btn_single_convert.UseVisualStyleBackColor = true;
            this.btn_single_convert.Click += new System.EventHandler(this.Btn_single_convert_Click);
            // 
            // track_bar_ocap
            // 
            resources.ApplyResources(this.track_bar_ocap, "track_bar_ocap");
            this.track_bar_ocap.Maximum = 100;
            this.track_bar_ocap.Name = "track_bar_ocap";
            this.track_bar_ocap.Value = 100;
            this.track_bar_ocap.ValueChanged += new System.EventHandler(this.Track_bar_ocap_ValueChanged);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.Controls.Add(this.label1);
            this.Controls.Add(this.track_bar_ocap);
            this.Controls.Add(this.btn_single_convert);
            this.Controls.Add(this.cb_reverse);
            this.Controls.Add(this.cb_text);
            this.Controls.Add(this.cb_ascii);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tb_data);
            this.Controls.Add(this.btn_save);
            this.Controls.Add(this.nud_yuzhi);
            this.Controls.Add(this.tb_yuzhi);
            this.Controls.Add(this.pb_to);
            this.Controls.Add(this.pb_src);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label_src_pic_name);
            this.Controls.Add(this.btn_mult_convert);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.HelpButton = true;
            this.Name = "MainForm";
            this.TransparencyKey = System.Drawing.SystemColors.Control;
            ((System.ComponentModel.ISupportInitialize)(this.pb_src)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_to)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_yuzhi)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.track_bar_ocap)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }


        #endregion

        private System.Windows.Forms.Button btn_mult_convert;
        private System.Windows.Forms.Label label_src_pic_name;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox pb_src;
        private System.Windows.Forms.PictureBox pb_to;
        private System.Windows.Forms.Label tb_yuzhi;
        private System.Windows.Forms.NumericUpDown nud_yuzhi;
        private System.Windows.Forms.Button btn_save;
        private System.Windows.Forms.TextBox tb_data;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox cb_ascii;
        private System.Windows.Forms.CheckBox cb_text;
        private System.Windows.Forms.CheckBox cb_reverse;
        private System.Windows.Forms.Button btn_single_convert;
        private System.Windows.Forms.TrackBar track_bar_ocap;
        private System.Windows.Forms.Label label1;
    }
}

