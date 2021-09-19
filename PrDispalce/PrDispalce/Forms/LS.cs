using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using AuxStructureLib;
using AuxStructureLib.IO;

using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.DataSourcesFile;

namespace PrDispalce.Forms
{
    public partial class LS : Form
    {
        public LS(AxMapControl axMapControl)
        {
            InitializeComponent();
            this.pMap = axMapControl.Map;
            this.pMapControl = axMapControl;
        }

        #region 参数
        IMap pMap;
        AxMapControl pMapControl;
        string OutPath;
        PrDispalce.PublicUtil.Symbolization Symbol = new PublicUtil.Symbolization();
        PrDispalce.PublicUtil.PolygonSimplify PS = new PublicUtil.PolygonSimplify();
        PrDispalce.PublicUtil.PolygonPreprocess PP = new PublicUtil.PolygonPreprocess();
        PrDispalce.PublicUtil.FeatureHandle pFeatureHandle = new PublicUtil.FeatureHandle();
        #endregion

        /// <summary>
        /// initialize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LS_Load(object sender, EventArgs e)
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
        /// 输出
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

            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }
            #endregion

            #region 数据读取并内插
            SMap map3 = new SMap(list);//测试用
            map3.ReadDateFrmEsriLyrsForEnrichNetWork();//测试用
            SMap map2 = new SMap();//测试用
            SMap map = new SMap(list);
            map.ReadDateFrmEsriLyrsForEnrichNetWork();

            PS.pMap = map2;//测试用
            #endregion

            #region Parameters
            #region BE parameters
            double BEScale = 0;
            if (this.comboBox3.Text == "")
            {
                MessageBox.Show("parameter not set, please check!");
                return;
            }
            else
            {
                BEScale = Convert.ToDouble(this.comboBox3.Text);
            }

            double MinLength = 0;
            if (this.textBox1.Text == "")
            {
                MessageBox.Show("parameter not set, please check!");
                return;
            }
            else
            {
                MinLength = Convert.ToDouble(this.textBox1.Text);
            }

            double MinWidth = 0;
            if (this.textBox2.Text == "")
            {
                MessageBox.Show("parameter not set, please check!");
                return;
            }
            else
            {
                MinWidth = Convert.ToDouble(this.textBox2.Text);
            }
            #endregion

            #region PreProcess Parameter
            double OnLineAngle = 0;
            double SharpAngle = 0;
            double RepeatDis = 0;

            if (this.textBox3.Text == "")
            {
                MessageBox.Show("parameter not set, please check!");
                return;
            }
            else
            {
                OnLineAngle = Convert.ToDouble(this.textBox3.Text) * Math.PI / 180;
            }

            if (this.textBox4.Text == "")
            {
                MessageBox.Show("parameter not set, please check!");
                return;
            }
            else
            {
                SharpAngle = Convert.ToDouble(this.textBox1.Text) * Math.PI / 180;
            }

            if (this.textBox5.Text == "")
            {
                MessageBox.Show("parameter not set, please check!");
                return;
            }
            else
            {
                RepeatDis = Convert.ToDouble(this.textBox5.Text);
            }
            #endregion

            #region LS Parameter
            double MinEdge = 0;
            double TA = 0;
            double TO = 0;
            double TP = 0;
            double OrthAngle = 0;

            if (this.textBox6.Text == "")
            {
                MessageBox.Show("parameter not set, please check!");
                return;
            }
            else
            {
                MinEdge = Convert.ToDouble(this.textBox6.Text);
            }

            if (this.textBox7.Text == "")
            {
                MessageBox.Show("parameter not set, please check!");
                return;
            }
            else
            {
                TA = Convert.ToDouble(this.textBox7.Text);
            }

            if (this.textBox8.Text == "")
            {
                MessageBox.Show("parameter not set, please check!");
                return;
            }
            else
            {
                TO = Convert.ToDouble(this.textBox8.Text);
            }

            if (this.textBox9.Text == "")
            {
                MessageBox.Show("parameter not set, please check!");
                return;
            }
            else
            {
                TP = Convert.ToDouble(this.textBox9.Text);
            }

            if (this.textBox10.Text == "")
            {
                MessageBox.Show("parameter not set, please check!");
                return;
            }
            else
            {
                OrthAngle = Convert.ToDouble(this.textBox10.Text) * Math.PI / 180;
            }
            #endregion
            #endregion

            #region 简化
            this.progressBar1.Maximum = map.PolygonList.Count - 1;
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                //Thread.Sleep(1000);//测试用

                PrDispalce.PublicUtil.FeatureSym Sb = new PublicUtil.FeatureSym();
                object PolygonSymbol = Sb.PolygonSymbolization(2, 100, 100, 100, 0, 0, 20, 20);
                IPolygon CacheMapPo = this.PolygonObjectConvert(map.PolygonList[i]);
                pMapControl.DrawShape(CacheMapPo, ref PolygonSymbol);
                pMapControl.Map.RecalcFullExtent();

                //map2.PolygonList.Add(map.PolygonList[i]);

                #region 符号化
                bool Label = false;
                map.PolygonList[i] = Symbol.SymbolizedPolygon(map.PolygonList[i], BEScale, MinLength, MinWidth, out Label);
                #endregion

                #region 简化
                if (!Label)
                {
                    PolygonObject CachePolygon1 = map.PolygonList[i];
                    PP.DeleteSamePoint(CachePolygon1, RepeatDis);
                    //map2.PolygonList.Add(CachePolygon1);//测试用

                    PP.DeleteOnLinePoint(CachePolygon1, OnLineAngle);
                    //map2.PolygonList.Add(CachePolygon1);//测试用

                    PP.DeleteSmallAngle(CachePolygon1, SharpAngle);


                    //PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
                    //object PolygonSymbol = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);
                    //IPolygon CacheCurPo = this.PolygonObjectConvert(CachePolygon1);
                    //pMapControl.DrawShape(CacheCurPo, ref PolygonSymbol);
                    //pMapControl.Map.RecalcFullExtent();

                    PolygonObject CachePolygon2 = CachePolygon1;
                    double ShortDis = 0;
                    List<TriNode> ShortestEdge = PP.GetShortestEdge(CachePolygon1, out ShortDis);
                    //ShortDis = 100;

                    #region 判断使简化后的最短边大于阈值
                    while (ShortDis < MinEdge && CachePolygon1.PointList.Count > 4)
                    {
                        bool sLabel = false;//false表示简化成功；true表示简化失败
                        CachePolygon1 = PS.PolygonSimplified3(map.PolygonList[i], CachePolygon1, OrthAngle, MinEdge, TA, TO, out sLabel);
                        if (CachePolygon1 != null)
                        {
                            PP.DeleteOnLinePoint(CachePolygon1, OnLineAngle);
                            PP.DeleteSamePoint(CachePolygon1, RepeatDis);
                            PP.DeleteSmallAngle(CachePolygon1, SharpAngle);

                            ShortestEdge = PP.GetShortestEdge(CachePolygon1, out ShortDis);
                            //ShortDis = 100;//测试用
                            map2.PolygonList.Add(CachePolygon1);//测试用
                            //map2.PolygonList.Add(CachePolygon1);//测试用

                            CachePolygon2 = CachePolygon1;
                        }
                        else
                        {
                            ShortDis = 100000;

                            if (sLabel)
                            {
                                CachePolygon2.SimLabel = 1;
                            }
                        }
                    }
                    #endregion

                    //添加测试用
                    //PP.DeleteOnLinePoint(CachePolygon2, Pi / 36);
                    //PP.DeleteSamePoint(CachePolygon2, 0.01);

                    map.PolygonList[i] = CachePolygon2;
                }
                #endregion

                this.progressBar1.Value = i;

            }
            #endregion

            #region 输出
            //map3.WriteResult2Shp(OutPath, pMap.SpatialReference);//测试用
            //map2.WriteResult2Shp(OutPath, pMap.SpatialReference);//测试用
            map.WriteResult2Shp(OutPath, pMap.SpatialReference);
            #endregion

            MessageBox.Show("Done!");
        }

        /// <summary>
        /// 将建筑物转化为IPolygon
        /// </summary>
        /// <param name="pPolygonObject"></param>
        /// <returns></returns>
        IPolygon PolygonObjectConvert(PolygonObject pPolygonObject)
        {
            Ring ring1 = new RingClass();
            object missing = Type.Missing;

            IPoint curResultPoint = new PointClass();
            TriNode curPoint = null;
            if (pPolygonObject != null)
            {
                for (int i = 0; i < pPolygonObject.PointList.Count; i++)
                {
                    curPoint = pPolygonObject.PointList[i];
                    curResultPoint.PutCoords(curPoint.X, curPoint.Y);
                    ring1.AddPoint(curResultPoint, ref missing, ref missing);
                }
            }

            curPoint = pPolygonObject.PointList[0];
            curResultPoint.PutCoords(curPoint.X, curPoint.Y);
            ring1.AddPoint(curResultPoint, ref missing, ref missing);

            IGeometryCollection pointPolygon = new PolygonClass();
            pointPolygon.AddGeometry(ring1 as IGeometry, ref missing, ref missing);
            IPolygon pPolygon = pointPolygon as IPolygon;

            //PrDispalce.工具类.Symbolization Sb = new 工具类.Symbolization();
            //object PolygonSymbol = Sb.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);

            //pMapControl.DrawShape(pPolygon, ref PolygonSymbol);
            //pMapControl.Map.RecalcFullExtent();

            pPolygon.SimplifyPreserveFromTo();
            return pPolygon;
        }

        /// <summary>
        /// polygon转换成polygonobject
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <returns></returns>
        public PolygonObject PolygonConvert(IPolygon pPolygon)
        {
            int ppID = 0;//（polygonobject自己的编号，应该无用）
            List<TriNode> trilist = new List<TriNode>();
            //Polygon的点集
            IPointCollection pointSet = pPolygon as IPointCollection;
            int count = pointSet.PointCount;
            double curX;
            double curY;
            //ArcGIS中，多边形的首尾点重复存储
            for (int i = 0; i < count - 1; i++)
            {
                curX = pointSet.get_Point(i).X;
                curY = pointSet.get_Point(i).Y;
                //初始化每个点对象
                TriNode tPoint = new TriNode(curX, curY, ppID, 1);
                trilist.Add(tPoint);
            }
            //生成自己写的多边形
            PolygonObject mPolygonObject = new PolygonObject(ppID, trilist);

            return mPolygonObject;
        }
    }
}
