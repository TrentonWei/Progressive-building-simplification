using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using AuxStructureLib;
using AuxStructureLib.IO;

using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.DataSourcesFile;

namespace PrDispalce.Forms
{
    public partial class BE : Form
    {
        public BE(AxMapControl axMapControl)
        {
            InitializeComponent();
            this.pMap = axMapControl.Map;
            this.pMapControl = axMapControl;
        }

        #region 参数
        IMap pMap;
        AxMapControl pMapControl;
        string OutPath;
        PrDispalce.PublicUtil.FeatureHandle pFeatureHandle = new PublicUtil.FeatureHandle();
        PrDispalce.PublicUtil.PolygonPreprocess PP = new PublicUtil.PolygonPreprocess();
        PrDispalce.PublicUtil.Symbolization Symbol = new PublicUtil.Symbolization();
        #endregion

        /// <summary>
        /// initialize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BE_Load(object sender, EventArgs e)
        {
            if (this.pMap.LayerCount <= 0)
                return;

            ILayer pLayer;
            string strLayerName;
            for (int i = 0; i < this.pMap.LayerCount; i++)
            {
                pLayer = this.pMap.get_Layer(i);
                strLayerName = pLayer.Name;

                IDataset LayerDataset = pLayer as IDataset;

                if (LayerDataset != null)
                {
                    IFeatureLayer pFeatureLayer = pLayer as IFeatureLayer;

                    #region add polygon Layer
                    if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        this.comboBox1.Items.Add(strLayerName);
                    }
                    #endregion
                }
            }

            this.comboBox3.Items.Add("25000");
            this.comboBox3.Items.Add("50000");

            #region First one to display by default
            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }
            if (this.comboBox3.Items.Count > 0)
            {
                this.comboBox3.SelectedIndex = 0;
            }
            #endregion
        }

        /// <summary>
        /// OutPut
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fdialog = new FolderBrowserDialog();
            string outfilepath = null;

            if (fdialog.ShowDialog() == DialogResult.OK)
            {
                string Path = fdialog.SelectedPath;
                outfilepath = Path;
            }

            OutPath = outfilepath;
            this.comboBox2.Text = OutPath;

        }

        /// <summary>
        /// algorithm application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            #region 获取图层
            List<IFeatureLayer> list = new List<IFeatureLayer>();
            if (this.comboBox2.Text != null)
            {
                IFeatureLayer StreetLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list.Add(StreetLayer);
            }

            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 参数获取
            double Scale = Convert.ToDouble(this.comboBox3.Text);
            double MinLength = Convert.ToDouble(this.textBox1.Text);
            double MinWidth = Convert.ToDouble(this.textBox2.Text);
            #endregion

            #region 数据读取并内插
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            this.progressBar1.Maximum = map.PolygonList.Count - 1;
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                double ShortDis = 0;
                List<TriNode> ShortestEdge = PP.GetShortestEdge(map.PolygonList[i], out ShortDis);

                #region 符号化
                bool Label = false;
                map.PolygonList[i] = Symbol.SymbolizedPolygon(map.PolygonList[i], Scale, MinLength, MinWidth, out Label);
                #endregion

                this.progressBar1.Value = i;
            }

            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
            MessageBox.Show("Done!");
        }
    }
}
