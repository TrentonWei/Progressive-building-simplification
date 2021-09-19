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
    public partial class TS : Form
    {
        public TS(AxMapControl axMapControl)
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
        PrDispalce.PublicUtil.ParameterCompute PC = new PublicUtil.ParameterCompute();
        PrDispalce.PublicUtil.ToolsForTS tTS = new PublicUtil.ToolsForTS();
        #endregion

        /// <summary>
        /// TS algorithm application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            #region 数据读取
            List<IFeatureLayer> list = new List<IFeatureLayer>();//查询建筑物图层
            if (this.comboBox1.Text != null)
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox1.Text);
                list.Add(BuildingLayer);
            }

            List<IFeatureLayer> list2 = new List<IFeatureLayer>();
            if (this.comboBox2.Text != null)//模板建筑物图层
            {
                IFeatureLayer BuildingLayer = pFeatureHandle.GetLayer(pMap, this.comboBox2.Text);
                list2.Add(BuildingLayer);
            }

            SMap map = new SMap(list);//原始建筑物图层
            map.ReadDateFrmEsriLyrsForEnrichNetWork();

            SMap map2 = new SMap(list2);//模板建筑物图层
            map2.ReadDateFrmEsriLyrsForEnrichNetWork();
            #endregion

            SMap OutMap = new SMap();

            #region 相似度计算
            this.progressBar1.Maximum = map.PolygonList.Count - 1;
            for (int i = 0; i < map.PolygonList.Count; i++)
            {
                List<Double> SimList = new List<double>();
                for (int j = 0; j < map2.PolygonList.Count; j++)
                {
                    double AngleBasedSim = this.GetPolygonSimBasedTuringAngle(map.PolygonList[i], map2.PolygonList[j]);
                    SimList.Add(AngleBasedSim);
                }

                double TargetSim = SimList.Min();

                #region Rotated/Scale/PAN
                IPolygon TargetPo = this.PolygonObjectConvert(map.PolygonList[i]);
                IPolygon MatchingPo=this.PolygonObjectConvert(map2.PolygonList[SimList.IndexOf(TargetSim)]);

                double tSBRO = PC.GetSMBROrientation(TargetPo);//TargetPo与Matching角度
                double mSBRO = PC.GetSMBROrientation(MatchingPo);
                IArea tArea = TargetPo as IArea;
                IArea mArea = MatchingPo as IArea;
     
                IPoint CenterPoint = tArea.Centroid;

                double rOri = (tSBRO - mSBRO) * Math.PI / 180;
                IPolygon rPolygon = tTS.GetRotatedPolygon(MatchingPo, rOri);
                IPolygon pPolygon = tTS.GetPannedPolygon(rPolygon, CenterPoint);
                IPolygon sPolygon = tTS.GetEnlargedPolygon(pPolygon, tArea.Area / mArea.Area);
                #endregion

                OutMap.PolygonList.Add(this.PolygonConvert(sPolygon));

                this.progressBar1.Value = i;
            }
            #endregion

            #region 输出
            OutMap.WriteResult2Shp(OutPath, pMap.SpatialReference);
            #endregion

            MessageBox.Show("Done!");
        }

        /// <summary>
        /// Output
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
            this.comboBox3.Text = OutPath;
        }

        /// <summary>
        /// initialize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TS_Load(object sender, EventArgs e)
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
                        this.comboBox2.Items.Add(strLayerName);
                    }
                    #endregion
                }
            }

            #region First one to display by default
            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }

            if (this.comboBox2.Items.Count > 0)
            {
                this.comboBox2.SelectedIndex = 0;
            }
            #endregion
        }

        /// <summary>
        /// 计算给定的两个建筑物的转角函数相似性
        /// </summary>
        /// <param name="Po1"></param>
        /// <param name="Po2"></param>
        /// <returns></returns>
        public double GetPolygonSimBasedTuringAngle(PolygonObject Po1, PolygonObject Po2)
        {
            double PolygonSimBasedTurningAngle = 0;

            #region 以不同起点计算相似程度
            List<double> TurningAngleList = new List<double>();
            for (int m = 0; m < Po1.PointList.Count; m++)
            {
                List<List<double>> TurningAngle1 = this.GetTurningAngle(Po1, m);

                for (int n = 0; n < Po2.PointList.Count; n++)
                {
                    List<List<double>> TurningAngle2 = this.GetTurningAngle(Po2, n);
                    double TurningAngleSim = this.GetTurningSim(TurningAngle1, TurningAngle2);
                    TurningAngleList.Add(TurningAngleSim);
                }
            }
            #endregion

            PolygonSimBasedTurningAngle = TurningAngleList.Min();//获得相似度最小的相似度
            return PolygonSimBasedTurningAngle;
        }

        /// <summary>
        /// 获得转角函数
        /// </summary>
        /// StartLocation=由于转角函数的起点选择对于相似度计算很大；所以StartLocation标记了起算点
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        List<List<double>> GetTurningAngle(PolygonObject pPolygon, int StartLocation)
        {
            List<List<double>> TurningAngle = new List<List<double>>();

            //获取角度
            pPolygon.GetBendAngle2(); double TotalAngle = 0;

            for (int i = 0; i < pPolygon.BendAngle.Count; i++)
            {
                List<double> tAngleDis = new List<double>();
                List<double> oAngleDis = pPolygon.BendAngle[(StartLocation + i) % pPolygon.BendAngle.Count];

                if (i == 0)
                {
                    tAngleDis.Add(0);//添加长度
                    tAngleDis.Add(oAngleDis[0] / pPolygon.Perimeter);//添加角度
                }

                if (i != 0)
                {
                    TotalAngle = TotalAngle + oAngleDis[1];
                    tAngleDis.Add(TotalAngle % (2 * Math.PI));//添加角度
                    tAngleDis.Add(oAngleDis[0] / pPolygon.Perimeter + TurningAngle[i - 1][1]);//添加长度
                }

                TurningAngle.Add(tAngleDis);
            }

            return TurningAngle;
        }

        /// <summary>
        /// 计算两个转角函数的相似度
        /// </summary>
        /// <param name="TurningAngle1"></param>
        /// <param name="TurningAngle2"></param>
        /// <returns></returns>
        double GetTurningSim(List<List<double>> TurningAngle1, List<List<double>> TurningAngle2)
        {
            double TurningSim = 0;

            List<double> CacheTurning1 = new List<double>();
            List<double> CacheTurning2 = new List<double>();
            int i = 0; int j = 0;
            double StartDis = 0; double EndDis = 0;

            while (Math.Abs(StartDis - 1) > 0.001 && Math.Abs(EndDis - 1) > 0.001)
            {
                CacheTurning1 = TurningAngle1[i];
                CacheTurning2 = TurningAngle2[j];

                if (CacheTurning1[1] < CacheTurning2[1])
                {
                    i++;
                    EndDis = CacheTurning1[1];

                    TurningSim = (EndDis - StartDis) * Math.Abs(CacheTurning1[0] - CacheTurning2[0]) + TurningSim;
                    StartDis = CacheTurning1[1];

                    //int TestLocation = 0;
                }

                else
                {
                    j++;
                    EndDis = CacheTurning2[1];

                    TurningSim = (EndDis - StartDis) * Math.Abs(CacheTurning1[0] - CacheTurning2[0]) + TurningSim;
                    StartDis = CacheTurning2[1];

                    //int TestLocaiton = 0;
                }
            }

            return TurningSim;
        }

        /// <summary>
        /// 将PolygonObject转化为IPolygon
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
        /// 将polygon转换成polygonobject
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
