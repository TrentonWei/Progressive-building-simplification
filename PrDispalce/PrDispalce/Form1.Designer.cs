﻿namespace PrDispalce
{
    partial class Form1
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
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.buildingSimplificationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aFToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rRToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bEToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.移除图层ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.axLicenseControl1 = new ESRI.ArcGIS.Controls.AxLicenseControl();
            this.axMapControl1 = new ESRI.ArcGIS.Controls.AxMapControl();
            this.axTOCControl1 = new ESRI.ArcGIS.Controls.AxTOCControl();
            this.axToolbarControl1 = new ESRI.ArcGIS.Controls.AxToolbarControl();
            this.tSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axLicenseControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.axMapControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.axTOCControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.axToolbarControl1)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.buildingSimplificationToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(9, 3, 0, 3);
            this.menuStrip1.Size = new System.Drawing.Size(1924, 34);
            this.menuStrip1.TabIndex = 5;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // buildingSimplificationToolStripMenuItem
            // 
            this.buildingSimplificationToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aFToolStripMenuItem,
            this.rRToolStripMenuItem,
            this.bEToolStripMenuItem,
            this.lSToolStripMenuItem,
            this.tSToolStripMenuItem});
            this.buildingSimplificationToolStripMenuItem.Name = "buildingSimplificationToolStripMenuItem";
            this.buildingSimplificationToolStripMenuItem.Size = new System.Drawing.Size(215, 28);
            this.buildingSimplificationToolStripMenuItem.Text = "Building simplification";
            // 
            // aFToolStripMenuItem
            // 
            this.aFToolStripMenuItem.Name = "aFToolStripMenuItem";
            this.aFToolStripMenuItem.Size = new System.Drawing.Size(152, 28);
            this.aFToolStripMenuItem.Text = "AF";
            this.aFToolStripMenuItem.Click += new System.EventHandler(this.aFToolStripMenuItem_Click);
            // 
            // rRToolStripMenuItem
            // 
            this.rRToolStripMenuItem.Name = "rRToolStripMenuItem";
            this.rRToolStripMenuItem.Size = new System.Drawing.Size(152, 28);
            this.rRToolStripMenuItem.Text = "RR";
            this.rRToolStripMenuItem.Click += new System.EventHandler(this.rRToolStripMenuItem_Click);
            // 
            // bEToolStripMenuItem
            // 
            this.bEToolStripMenuItem.Name = "bEToolStripMenuItem";
            this.bEToolStripMenuItem.Size = new System.Drawing.Size(152, 28);
            this.bEToolStripMenuItem.Text = "BE";
            this.bEToolStripMenuItem.Click += new System.EventHandler(this.bEToolStripMenuItem_Click);
            // 
            // lSToolStripMenuItem
            // 
            this.lSToolStripMenuItem.Name = "lSToolStripMenuItem";
            this.lSToolStripMenuItem.Size = new System.Drawing.Size(152, 28);
            this.lSToolStripMenuItem.Text = "LS";
            this.lSToolStripMenuItem.Click += new System.EventHandler(this.lSToolStripMenuItem_Click);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.移除图层ToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(153, 32);
            // 
            // 移除图层ToolStripMenuItem
            // 
            this.移除图层ToolStripMenuItem.Name = "移除图层ToolStripMenuItem";
            this.移除图层ToolStripMenuItem.Size = new System.Drawing.Size(152, 28);
            this.移除图层ToolStripMenuItem.Text = "移除图层";
            // 
            // axLicenseControl1
            // 
            this.axLicenseControl1.Enabled = true;
            this.axLicenseControl1.Location = new System.Drawing.Point(740, 376);
            this.axLicenseControl1.Margin = new System.Windows.Forms.Padding(4);
            this.axLicenseControl1.Name = "axLicenseControl1";
            this.axLicenseControl1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axLicenseControl1.OcxState")));
            this.axLicenseControl1.Size = new System.Drawing.Size(32, 32);
            this.axLicenseControl1.TabIndex = 9;
            // 
            // axMapControl1
            // 
            this.axMapControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axMapControl1.Location = new System.Drawing.Point(264, 62);
            this.axMapControl1.Margin = new System.Windows.Forms.Padding(4);
            this.axMapControl1.Name = "axMapControl1";
            this.axMapControl1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axMapControl1.OcxState")));
            this.axMapControl1.Size = new System.Drawing.Size(1660, 852);
            this.axMapControl1.TabIndex = 8;
            // 
            // axTOCControl1
            // 
            this.axTOCControl1.Dock = System.Windows.Forms.DockStyle.Left;
            this.axTOCControl1.Location = new System.Drawing.Point(0, 62);
            this.axTOCControl1.Margin = new System.Windows.Forms.Padding(4);
            this.axTOCControl1.Name = "axTOCControl1";
            this.axTOCControl1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axTOCControl1.OcxState")));
            this.axTOCControl1.Size = new System.Drawing.Size(264, 852);
            this.axTOCControl1.TabIndex = 7;
            // 
            // axToolbarControl1
            // 
            this.axToolbarControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.axToolbarControl1.Location = new System.Drawing.Point(0, 34);
            this.axToolbarControl1.Margin = new System.Windows.Forms.Padding(4);
            this.axToolbarControl1.Name = "axToolbarControl1";
            this.axToolbarControl1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axToolbarControl1.OcxState")));
            this.axToolbarControl1.Size = new System.Drawing.Size(1924, 28);
            this.axToolbarControl1.TabIndex = 6;
            // 
            // tSToolStripMenuItem
            // 
            this.tSToolStripMenuItem.Name = "tSToolStripMenuItem";
            this.tSToolStripMenuItem.Size = new System.Drawing.Size(152, 28);
            this.tSToolStripMenuItem.Text = "TS";
            this.tSToolStripMenuItem.Click += new System.EventHandler(this.tSToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1924, 914);
            this.Controls.Add(this.axLicenseControl1);
            this.Controls.Add(this.axMapControl1);
            this.Controls.Add(this.axTOCControl1);
            this.Controls.Add(this.axToolbarControl1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.Text = "Form1";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.axLicenseControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.axMapControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.axTOCControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.axToolbarControl1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 移除图层ToolStripMenuItem;
        private ESRI.ArcGIS.Controls.AxToolbarControl axToolbarControl1;
        private ESRI.ArcGIS.Controls.AxTOCControl axTOCControl1;
        private ESRI.ArcGIS.Controls.AxMapControl axMapControl1;
        private ESRI.ArcGIS.Controls.AxLicenseControl axLicenseControl1;
        private System.Windows.Forms.ToolStripMenuItem buildingSimplificationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aFToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rRToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bEToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lSToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tSToolStripMenuItem;
    }
}

