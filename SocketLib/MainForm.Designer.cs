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
            this.txt_ipAddr1 = new System.Windows.Forms.TextBox();
            this.btn_listen = new System.Windows.Forms.Button();
            this.txt_portno1 = new System.Windows.Forms.TextBox();
            this.btn_connect = new System.Windows.Forms.Button();
            this.txt_log = new System.Windows.Forms.TextBox();
            this.txt_portno2 = new System.Windows.Forms.TextBox();
            this.txt_ipAddr2 = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // txt_ipAddr1
            // 
            this.txt_ipAddr1.Location = new System.Drawing.Point(37, 30);
            this.txt_ipAddr1.Name = "txt_ipAddr1";
            this.txt_ipAddr1.Size = new System.Drawing.Size(165, 31);
            this.txt_ipAddr1.TabIndex = 0;
            this.txt_ipAddr1.Text = "192.168.179.2";
            // 
            // btn_listen
            // 
            this.btn_listen.Location = new System.Drawing.Point(453, 26);
            this.btn_listen.Name = "btn_listen";
            this.btn_listen.Size = new System.Drawing.Size(122, 38);
            this.btn_listen.TabIndex = 1;
            this.btn_listen.Text = "listen";
            this.btn_listen.UseVisualStyleBackColor = true;
            this.btn_listen.Click += new System.EventHandler(this.btn_listen_Click);
            // 
            // txt_portno1
            // 
            this.txt_portno1.Location = new System.Drawing.Point(222, 30);
            this.txt_portno1.Name = "txt_portno1";
            this.txt_portno1.Size = new System.Drawing.Size(100, 31);
            this.txt_portno1.TabIndex = 2;
            // 
            // btn_connect
            // 
            this.btn_connect.Location = new System.Drawing.Point(453, 77);
            this.btn_connect.Name = "btn_connect";
            this.btn_connect.Size = new System.Drawing.Size(122, 38);
            this.btn_connect.TabIndex = 3;
            this.btn_connect.Text = "connect";
            this.btn_connect.UseVisualStyleBackColor = true;
            this.btn_connect.Click += new System.EventHandler(this.btn_connect_Click);
            // 
            // txt_log
            // 
            this.txt_log.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txt_log.Location = new System.Drawing.Point(0, 0);
            this.txt_log.Multiline = true;
            this.txt_log.Name = "txt_log";
            this.txt_log.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txt_log.Size = new System.Drawing.Size(800, 286);
            this.txt_log.TabIndex = 4;
            // 
            // txt_portno2
            // 
            this.txt_portno2.Location = new System.Drawing.Point(222, 84);
            this.txt_portno2.Name = "txt_portno2";
            this.txt_portno2.Size = new System.Drawing.Size(100, 31);
            this.txt_portno2.TabIndex = 6;
            // 
            // txt_ipAddr2
            // 
            this.txt_ipAddr2.Location = new System.Drawing.Point(37, 84);
            this.txt_ipAddr2.Name = "txt_ipAddr2";
            this.txt_ipAddr2.Size = new System.Drawing.Size(165, 31);
            this.txt_ipAddr2.TabIndex = 5;
            this.txt_ipAddr2.Text = "192.168.179.2";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.txt_ipAddr1);
            this.panel1.Controls.Add(this.txt_portno2);
            this.panel1.Controls.Add(this.btn_listen);
            this.panel1.Controls.Add(this.txt_ipAddr2);
            this.panel1.Controls.Add(this.txt_portno1);
            this.panel1.Controls.Add(this.btn_connect);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(800, 164);
            this.panel1.TabIndex = 7;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.txt_log);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 164);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(800, 286);
            this.panel2.TabIndex = 8;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox txt_ipAddr1;
        private System.Windows.Forms.Button btn_listen;
        private System.Windows.Forms.TextBox txt_portno1;
        private System.Windows.Forms.Button btn_connect;
        private System.Windows.Forms.TextBox txt_log;
        private System.Windows.Forms.TextBox txt_portno2;
        private System.Windows.Forms.TextBox txt_ipAddr2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
    }
}

