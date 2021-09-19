using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;

namespace PrDispalce
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        #region 参数
        ILayer pLayer;
        #endregion

        /// <summary>
        /// AF algorithm frm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.Forms.AF AFfrm = new Forms.AF(this.axMapControl1);
            AFfrm.Show();
        }

        /// <summary>
        /// RR algorithm frm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rRToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.Forms.RR RRfrm = new Forms.RR(this.axMapControl1);
            RRfrm.Show();
        }

        /// <summary>
        /// BE algorithm frm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.Forms.BE BEfrm = new Forms.BE(this.axMapControl1);
            BEfrm.Show();
        }

        /// <summary>
        /// LS algorithm frm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.Forms.LS LSfrm = new Forms.LS(this.axMapControl1);
            LSfrm.Show();
        }

        /// <summary>
        /// TS algorithm frm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrDispalce.Forms.TS TSfrm = new Forms.TS(this.axMapControl1);
            TSfrm.Show();
        }    
    }
}
