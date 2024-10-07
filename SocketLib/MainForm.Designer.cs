namespace SampleMain
{
    partial class MainForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.txt_ipAddr = new System.Windows.Forms.TextBox();
            this.btn_listen = new System.Windows.Forms.Button();
            this.txt_portno = new System.Windows.Forms.TextBox();
            this.btn_connect = new System.Windows.Forms.Button();
            this.txt_log = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // txt_ipAddr
            // 
            this.txt_ipAddr.Location = new System.Drawing.Point(72, 30);
            this.txt_ipAddr.Name = "txt_ipAddr";
            this.txt_ipAddr.Size = new System.Drawing.Size(165, 31);
            this.txt_ipAddr.TabIndex = 0;
            this.txt_ipAddr.Text = "192.168.179.2";
            // 
            // btn_listen
            // 
            this.btn_listen.Location = new System.Drawing.Point(488, 26);
            this.btn_listen.Name = "btn_listen";
            this.btn_listen.Size = new System.Drawing.Size(122, 38);
            this.btn_listen.TabIndex = 1;
            this.btn_listen.Text = "listen";
            this.btn_listen.UseVisualStyleBackColor = true;
            this.btn_listen.Click += new System.EventHandler(this.btn_listen_Click);
            // 
            // txt_portno
            // 
            this.txt_portno.Location = new System.Drawing.Point(257, 26);
            this.txt_portno.Name = "txt_portno";
            this.txt_portno.Size = new System.Drawing.Size(100, 31);
            this.txt_portno.TabIndex = 2;
            // 
            // btn_connect
            // 
            this.btn_connect.Location = new System.Drawing.Point(648, 23);
            this.btn_connect.Name = "btn_connect";
            this.btn_connect.Size = new System.Drawing.Size(122, 38);
            this.btn_connect.TabIndex = 3;
            this.btn_connect.Text = "connect";
            this.btn_connect.UseVisualStyleBackColor = true;
            this.btn_connect.Click += new System.EventHandler(this.btn_connect_Click);
            // 
            // txt_log
            // 
            this.txt_log.Location = new System.Drawing.Point(12, 151);
            this.txt_log.Multiline = true;
            this.txt_log.Name = "txt_log";
            this.txt_log.Size = new System.Drawing.Size(776, 210);
            this.txt_log.TabIndex = 4;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.txt_log);
            this.Controls.Add(this.btn_connect);
            this.Controls.Add(this.txt_portno);
            this.Controls.Add(this.btn_listen);
            this.Controls.Add(this.txt_ipAddr);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txt_ipAddr;
        private System.Windows.Forms.Button btn_listen;
        private System.Windows.Forms.TextBox txt_portno;
        private System.Windows.Forms.Button btn_connect;
        private System.Windows.Forms.TextBox txt_log;
    }
}

