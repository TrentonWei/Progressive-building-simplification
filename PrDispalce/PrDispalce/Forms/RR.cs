using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.DataSourcesFile;

namespace PrDispalce.Forms
{
    public partial class RR : Form
    {
        public RR(AxMapControl axMapControl)
        {
            InitializeComponent();
            this.pMap = axMapControl.Map;
            this.pMapControl = axMapControl;
        }

        #region Parameters
        AxMapControl pMapControl;
        IMap pMap;
        PrDispalce.PublicUtil.FeatureHandle pFeatureHandle = new PublicUtil.FeatureHandle();
        string OutPath;
        static List<Edge> edge_List = new List<Edge>();
        static List<IPointCollection> PtInEdge = new List<IPointCollection>();
        static List<Edge> OutPutList = new List<Edge>();
        static double Threshold;
        static double Sigma;
        #endregion

        /// <summary>
        /// initialize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RR_Load(object sender, EventArgs e)
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

            #region First one to display by default
            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }
            #endregion
        }

        /// <summary>
        /// OutPut
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
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

        static private bool IsShortEdgeExit(IPointCollection pPtInPlg)
        {
            double minLength = double.MaxValue;
            for (int i = 0; i < pPtInPlg.PointCount - 1; i++)
            {
                double length = Length(pPtInPlg.get_Point(i), pPtInPlg.get_Point(i + 1));
                if (minLength > length)
                    minLength = length;
            }
            if (minLength > Threshold)
                return false;
            else
                return true;
        }

        static private int JudgeGeometryNormal(IGeometry geo)
        {
            IPolygon polygon = geo as IPolygon;
            IPolygon4 poly = polygon as IPolygon4;
            if (poly.ExteriorRingCount > 1) return 0;
            else
            {
                IGeometryBag bag = poly.ExteriorRingBag;
                IEnumGeometry enumGeo = bag as IEnumGeometry;
                enumGeo.Reset();
                IRing exRing = null;
                int innerRingCount = 0;
                while ((exRing = enumGeo.Next() as IRing) != null)
                {
                    innerRingCount = innerRingCount + poly.get_InteriorRingCount(exRing);
                    break;
                }
                if (innerRingCount > 0) return 1;
                else
                    return -1;
            }
        }

        static private IFeatureClass OpenFeatClass(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            IWorkspace workspace;
            IFeatureWorkspace featureWorkspace;
            IFeatureClass featureClass;

            IWorkspaceFactory workspaceFactory = new ShapefileWorkspaceFactoryClass();
            workspace = workspaceFactory.OpenFromFile(System.IO.Path.GetDirectoryName(path), 0);
            featureWorkspace = workspace as IFeatureWorkspace;
            string filename = System.IO.Path.GetFileNameWithoutExtension(path);

            featureClass = featureWorkspace.OpenFeatureClass(filename);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(featureWorkspace);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(workspaceFactory);
            if (featureClass != null)
            {
                return featureClass;
            }
            return null;
        }

        private class Edge
        {
            int orientation;
            int r;
            int start;
            int end;
            double x;
            double y;
            double sigma;
            int type;
            int motherEdge;

            public int Orientation
            {
                get { return orientation; }
                set { orientation = value; }
            }
            public int R
            {
                get { return r; }
                set { r = value; }
            }
            public int Start
            {
                get { return start; }
                set { start = value; }
            }
            public int End
            {
                get { return end; }
                set { end = value; }
            }
            public double X
            {
                get { return x; }
                set { x = value; }
            }
            public double Y
            {
                get { return y; }
                set { y = value; }
            }
            public double SIGMA
            {
                get { return sigma; }
                set { sigma = value; }
            }
            public int Type
            {
                get { return type; }
                set { type = value; }
            }
            public int MotherEdge
            {
                get { return motherEdge; }
                set { motherEdge = value; }
            }
        }

        static private IPolygon MBR(IPolygon pPoly)
        {
            IPolygon m_Poly = new PolygonClass();
            IPointCollection pPtInPolygon = pPoly as IPointCollection;
            IPointCollection pPt = new PolygonClass();
            IPointCollection pPtInRect = new PolygonClass();
            List<double> terminal = new List<double>();
            List<double> Ang = new List<double>();
            IPoint Org, temp;
            Org = new PointClass();
            temp = new PointClass();
            Org.X = 0; Org.Y = 0;
            Terminal(pPtInPolygon, terminal);
            double min_Area = (terminal[2] - terminal[0]) * (terminal[3] - terminal[1]);
            for (int i = 0; i < pPtInPolygon.PointCount - 1; i++)
            {
                temp.X = pPtInPolygon.get_Point(i + 1).X - pPtInPolygon.get_Point(i).X;
                temp.Y = pPtInPolygon.get_Point(i + 1).Y - pPtInPolygon.get_Point(i).Y;
                if (temp.X * 0 + temp.Y * 1 > 0 && temp.X > 0)
                    Ang.Add(System.Math.Acos((temp.X * 0 + temp.Y * 1) / Length(Org, temp)));
                if (temp.X * 0 + temp.Y * -1 > 0 && temp.X < 0)
                    Ang.Add(System.Math.Acos((temp.X * 0 + temp.Y * -1) / Length(Org, temp)));
                if (temp.X * -1 + temp.Y * 0 > 0 && temp.Y > 0)
                    Ang.Add(System.Math.Acos((temp.X * -1 + temp.Y * 0) / Length(Org, temp)));
                if (temp.X * 1 + temp.Y * 0 > 0 && temp.Y < 0)
                    Ang.Add(System.Math.Acos((temp.X * 1 + temp.Y * 0) / Length(Org, temp)));
            }
            temp.X = terminal[0]; temp.Y = terminal[1];
            pPtInRect.AddPoint(temp);
            temp.X = terminal[0]; temp.Y = terminal[3];
            pPtInRect.AddPoint(temp);
            temp.X = terminal[2]; temp.Y = terminal[3];
            pPtInRect.AddPoint(temp);
            temp.X = terminal[2]; temp.Y = terminal[1];
            pPtInRect.AddPoint(temp);
            for (int i = 0; i < Ang.Count; i++)
            {
                for (int j = 0; j < pPtInPolygon.PointCount; j++)
                {
                    temp.X = pPtInPolygon.get_Point(j).X * System.Math.Cos(Ang[i]) - pPtInPolygon.get_Point(j).Y * System.Math.Sin(Ang[i]);
                    temp.Y = pPtInPolygon.get_Point(j).X * System.Math.Sin(Ang[i]) + pPtInPolygon.get_Point(j).Y * System.Math.Cos(Ang[i]);
                    pPt.AddPoint(temp);
                }
                terminal.Clear();
                Terminal(pPt, terminal);
                pPt.RemovePoints(0, pPt.PointCount);
                if (min_Area > (terminal[2] - terminal[0]) * (terminal[3] - terminal[1]))
                {
                    min_Area = (terminal[2] - terminal[0]) * (terminal[3] - terminal[1]);
                    pPtInRect.RemovePoints(0, 4);
                    temp.X = terminal[0]; temp.Y = terminal[1];
                    pPtInRect.AddPoint(temp);
                    temp.X = terminal[0]; temp.Y = terminal[3];
                    pPtInRect.AddPoint(temp);
                    temp.X = terminal[2]; temp.Y = terminal[3];
                    pPtInRect.AddPoint(temp);
                    temp.X = terminal[2]; temp.Y = terminal[1];
                    pPtInRect.AddPoint(temp);
                    for (int k = 0; k < 4; k++)
                    {
                        temp.X = pPtInRect.get_Point(k).X * System.Math.Cos(-Ang[i]) - pPtInRect.get_Point(k).Y * System.Math.Sin(-Ang[i]);
                        temp.Y = pPtInRect.get_Point(k).X * System.Math.Sin(-Ang[i]) + pPtInRect.get_Point(k).Y * System.Math.Cos(-Ang[i]);
                        pPtInRect.AddPoint(temp);
                    }
                    pPtInRect.RemovePoints(0, 4);
                }
            }
            pPtInRect.AddPoint(pPtInRect.get_Point(0));
            m_Poly = pPtInRect as IPolygon;
            return m_Poly;
        }

        static private IPoint FindCore(IPolygon pPoly)
        {
            IPoint m_pt = new PointClass();
            m_pt = (pPoly as IArea).Centroid;
            m_pt.Z = (pPoly as IArea).Area;
            return m_pt;
        }

        static private IPolygon Zoom(IPolygon pPoly, IPoint pPt)
        {
            IPointCollection pPtInPolygon = pPoly as IPointCollection;
            IPointCollection pPtInRect = new PolygonClass();
            IPoint rect_Core = new PointClass();
            IPoint temp = new PointClass();
            rect_Core = FindCore(pPoly);
            IPolygon end_Poly = new PolygonClass();
            for (int i = 0; i < pPtInPolygon.PointCount; i++)
            {
                temp.X = pPtInPolygon.get_Point(i).X - rect_Core.X + pPt.X;
                temp.Y = pPtInPolygon.get_Point(i).Y - rect_Core.Y + pPt.Y;
                temp.X = pPt.X + (temp.X - pPt.X) * System.Math.Sqrt(pPt.Z / rect_Core.Z);
                temp.Y = pPt.Y + (temp.Y - pPt.Y) * System.Math.Sqrt(pPt.Z / rect_Core.Z);
                pPtInRect.AddPoint(temp);
            }
            end_Poly = pPtInRect as IPolygon;
            return end_Poly;
        }

        static private double Length(IPoint pPt1, IPoint pPt2)
        {
            double length = System.Math.Sqrt((pPt2.X - pPt1.X) * (pPt2.X - pPt1.X) + (pPt2.Y - pPt1.Y) * (pPt2.Y - pPt1.Y));
            return length;
        }

        static private void Terminal(IPointCollection pPtInPolygon, List<double> terminal)
        {
            double Xmin = double.MaxValue;
            double Ymin = double.MaxValue;
            double Xmax = double.MinValue;
            double Ymax = double.MinValue;
            for (int i = 0; i < pPtInPolygon.PointCount; i++)
            {
                if (Xmin > pPtInPolygon.get_Point(i).X)
                    Xmin = pPtInPolygon.get_Point(i).X;
                if (Ymin > pPtInPolygon.get_Point(i).Y)
                    Ymin = pPtInPolygon.get_Point(i).Y;
                if (Xmax < pPtInPolygon.get_Point(i).X)
                    Xmax = pPtInPolygon.get_Point(i).X;
                if (Ymax < pPtInPolygon.get_Point(i).Y)
                    Ymax = pPtInPolygon.get_Point(i).Y;
            }
            terminal.Add(Xmin);
            terminal.Add(Ymin);
            terminal.Add(Xmax);
            terminal.Add(Ymax);
        }

        static private void Rotate(IPolygon plg1, double angle)
        {
            IPointCollection pPtInPlg1 = plg1 as IPointCollection;
            for (int i = 0; i < pPtInPlg1.PointCount - i; i++)
            {
                IPoint temp = new PointClass();
                temp.X = pPtInPlg1.get_Point(i).X * System.Math.Cos(angle) + pPtInPlg1.get_Point(i).Y * System.Math.Sin(angle);
                temp.Y = -pPtInPlg1.get_Point(i).X * System.Math.Sin(angle) + pPtInPlg1.get_Point(i).Y * System.Math.Cos(angle);
                pPtInPlg1.AddPoint(temp);
            }
            pPtInPlg1.RemovePoints(0, pPtInPlg1.PointCount / 2);
        }

        static private void FindEgde(IPolygon plg1, IPolygon plg2)
        {
            IPointCollection pPtInPlg1 = plg1 as IPointCollection;
            IPointCollection pPtInPlg2 = plg2 as IPointCollection;
            int flag;
            if (pPtInPlg1.get_Point(0).X < pPtInPlg1.get_Point(1).X)
            {
                if (pPtInPlg1.get_Point(0).Y < pPtInPlg1.get_Point(3).Y)
                    flag = 3;
                else
                    flag = 0;
            }
            else
            {
                if (pPtInPlg1.get_Point(1).Y < pPtInPlg1.get_Point(2).Y)
                    flag = 2;
                else
                    flag = 1;
            }
            for (int i = flag; i < pPtInPlg1.PointCount - i - 1; i++)
                pPtInPlg1.AddPoint(pPtInPlg1.get_Point(i));
            for (int i = 0; i < flag + 1; i++)
                pPtInPlg1.AddPoint(pPtInPlg1.get_Point(i));
            pPtInPlg1.RemovePoints(0, pPtInPlg1.PointCount / 2);
            double min_length = double.MaxValue;
            for (int i = 0; i < pPtInPlg1.PointCount - 1; i++)
            {
                Edge edge = new Edge();
                for (int j = 0; j < pPtInPlg2.PointCount - 1; j++)
                {
                    double length = Length(pPtInPlg1.get_Point(i), pPtInPlg2.get_Point(j));
                    if (min_length > length)
                    {
                        min_length = length;
                        edge.Start = j;
                    }
                }
                edge.Orientation = i + 1;
                edge.R = 1;
                edge_List.Add(edge);
                min_length = double.MaxValue;
            }
            for (int i = 0; i < edge_List.Count; i++)
            {
                IPointCollection temp = new PolylineClass();
                if (edge_List[i].Start > edge_List[(i + 1) % edge_List.Count].Start)
                {
                    for (int j = edge_List[i].Start; j < pPtInPlg2.PointCount - 1; j++)
                        temp.AddPoint(pPtInPlg2.get_Point(j));
                    for (int j = 0; j <= edge_List[(i + 1) % edge_List.Count].Start; j++)
                        temp.AddPoint(pPtInPlg2.get_Point(j));
                }
                else
                {
                    for (int j = edge_List[i].Start; j <= edge_List[(i + 1) % edge_List.Count].Start; j++)
                        temp.AddPoint(pPtInPlg2.get_Point(j));
                }
                PtInEdge.Add(temp);
                edge_List[i].End = edge_List[(i + 1) % edge_List.Count].Start;
                edge_List[i].MotherEdge = i + 1;
            }
        }

        static private void EdgeSplitting(IPointCollection pPtInPlg, Edge edge, int n)
        {
            //if (pPtInPlg.PointCount == 3)
            //    pPtInPlg.RemovePoints(1,1);
            RegressionLine(pPtInPlg, edge);
            if (edge.SIGMA < Sigma || pPtInPlg.PointCount == 2 || pPtInPlg.PointCount == 3)
            {
                OutPutList.Add(edge);
                edge_List.RemoveAt(0);
                PtInEdge.RemoveAt(0);
            }
            else
            {
                TemporarySplitting(pPtInPlg, edge, n);
            }
        }

        static private void TemporarySplitting(IPointCollection pPtInPlg, Edge edge, int n)
        {
            List<IPointCollection> temp_plgList = new List<IPointCollection>();
            List<Edge> temp_edgeList = new List<Edge>();
            #region
            //int flag = 0;
            for (int flag = 0; flag < pPtInPlg.PointCount - 1; )
            {
                if (edge.Orientation == 1 || edge.Orientation == 3)
                {
                    if ((pPtInPlg.get_Point(flag).Y >= edge.Y))
                    {
                        IPointCollection temp_plg = new PolylineClass();
                        Edge temp_edge = new Edge();
                        for (int i = flag; i < pPtInPlg.PointCount; i++)
                        {
                            if (pPtInPlg.get_Point(i).Y >= edge.Y)
                                temp_plg.AddPoint(pPtInPlg.get_Point(i));
                            else
                            {
                                temp_edge.Start = (edge.Start + flag) % (n - 1);
                                temp_edge.End = (edge.Start + i - 1) % (n - 1);
                                temp_edge.Orientation = edge.Orientation;
                                temp_edge.R = edge.R + 1;
                                temp_plgList.Add(temp_plg);
                                temp_edgeList.Add(temp_edge);
                                flag = i;
                                break;
                            }
                            if (i == (pPtInPlg.PointCount - 1))
                            {
                                temp_edge.Start = (edge.Start + flag) % (n - 1);
                                temp_edge.End = (edge.Start + i) % (n - 1);
                                temp_edge.Orientation = edge.Orientation;
                                temp_edge.R = edge.R + 1;
                                temp_plgList.Add(temp_plg);
                                temp_edgeList.Add(temp_edge);
                                flag = i;
                                break;
                            }
                        }
                    }
                    if ((pPtInPlg.get_Point(flag).Y < edge.Y))
                    {
                        IPointCollection temp_plg = new PolylineClass();
                        Edge temp_edge = new Edge();
                        for (int i = flag; i < pPtInPlg.PointCount; i++)
                        {
                            if (pPtInPlg.get_Point(i).Y < edge.Y)
                                temp_plg.AddPoint(pPtInPlg.get_Point(i));
                            else
                            {
                                temp_edge.Start = (edge.Start + flag) % (n - 1);
                                temp_edge.End = (edge.Start + i - 1) % (n - 1);
                                temp_edge.Orientation = edge.Orientation;
                                temp_edge.R = edge.R + 1;
                                temp_plgList.Add(temp_plg);
                                temp_edgeList.Add(temp_edge);
                                flag = i;
                                break;
                            }
                            if (i == (pPtInPlg.PointCount - 1))
                            {
                                temp_edge.Start = (edge.Start + flag) % (n - 1);
                                temp_edge.End = (edge.Start + i) % (n - 1);
                                temp_edge.Orientation = edge.Orientation;
                                temp_edge.R = edge.R + 1;
                                temp_plgList.Add(temp_plg);
                                temp_edgeList.Add(temp_edge);
                                flag = i;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if ((pPtInPlg.get_Point(flag).X >= edge.X))
                    {
                        IPointCollection temp_plg = new PolylineClass();
                        Edge temp_edge = new Edge();
                        for (int i = flag; i < pPtInPlg.PointCount; i++)
                        {
                            if (pPtInPlg.get_Point(i).X >= edge.X)
                                temp_plg.AddPoint(pPtInPlg.get_Point(i));
                            else
                            {
                                temp_edge.Start = (edge.Start + flag) % (n - 1);
                                temp_edge.End = (edge.Start + i - 1) % (n - 1);
                                temp_edge.Orientation = edge.Orientation;
                                temp_edge.R = edge.R + 1;
                                temp_plgList.Add(temp_plg);
                                temp_edgeList.Add(temp_edge);
                                flag = i;
                                break;
                            }
                            if (i == (pPtInPlg.PointCount - 1))
                            {
                                temp_edge.Start = (edge.Start + flag) % (n - 1);
                                temp_edge.End = (edge.Start + i) % (n - 1);
                                temp_edge.Orientation = edge.Orientation;
                                temp_edge.R = edge.R + 1;
                                temp_plgList.Add(temp_plg);
                                temp_edgeList.Add(temp_edge);
                                flag = i;
                                break;
                            }
                        }
                    }
                    if ((pPtInPlg.get_Point(flag).X < edge.X))
                    {
                        IPointCollection temp_plg = new PolylineClass();
                        Edge temp_edge = new Edge();
                        for (int i = flag; i < pPtInPlg.PointCount; i++)
                        {
                            if (pPtInPlg.get_Point(i).X < edge.X)
                                temp_plg.AddPoint(pPtInPlg.get_Point(i));
                            else
                            {
                                temp_edge.Start = (edge.Start + flag) % (n - 1);
                                temp_edge.End = (edge.Start + i - 1) % (n - 1);
                                temp_edge.Orientation = edge.Orientation;
                                temp_edge.R = edge.R + 1;
                                temp_plgList.Add(temp_plg);
                                temp_edgeList.Add(temp_edge);
                                flag = i;
                                break;
                            }
                            if (i == (pPtInPlg.PointCount - 1))
                            {
                                temp_edge.Start = (edge.Start + flag) % (n - 1);
                                temp_edge.End = (edge.Start + i) % (n - 1);
                                temp_edge.Orientation = edge.Orientation;
                                temp_edge.R = edge.R + 1;
                                temp_plgList.Add(temp_plg);
                                temp_edgeList.Add(temp_edge);
                                flag = i;
                                break;
                            }
                        }
                    }
                }
            }
            #endregion
            if (temp_plgList.Count == 0)
            {
                pPtInPlg.RemovePoints(0, 1);
                edge.Start = edge.Start + 1;
                temp_edgeList.Add(edge);
                temp_plgList.Add(pPtInPlg);
            }
            else
            {
                #region
                if (edge.Orientation == 1)
                {
                    if (temp_plgList.Count % 2 == 1)
                    {
                        if (temp_plgList[0].get_Point(0).Y >= edge.Y)
                        {
                            temp_edgeList[0].Type = 1;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 3;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 6;
                                else
                                    temp_edgeList[i].Type = 5;
                            }
                        }
                        else
                        {
                            temp_edgeList[0].Type = 2;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 4;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 5;
                                else
                                    temp_edgeList[i].Type = 6;
                            }
                        }
                    }
                    else
                    {
                        if (temp_plgList[0].get_Point(0).Y >= edge.Y)
                        {
                            temp_edgeList[0].Type = 1;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 4;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 6;
                                else
                                    temp_edgeList[i].Type = 5;
                            }
                        }
                        else
                        {
                            temp_edgeList[0].Type = 2;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 3;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 5;
                                else
                                    temp_edgeList[i].Type = 6;
                            }
                        }
                    }
                }
                else if (edge.Orientation == 2)
                {
                    if (temp_plgList.Count % 2 == 1)
                    {
                        if (temp_plgList[0].get_Point(0).X >= edge.X)
                        {
                            temp_edgeList[0].Type = 1;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 3;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 6;
                                else
                                    temp_edgeList[i].Type = 5;
                            }
                        }
                        else
                        {
                            temp_edgeList[0].Type = 2;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 4;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 5;
                                else
                                    temp_edgeList[i].Type = 6;
                            }
                        }
                    }
                    else
                    {
                        if (temp_plgList[0].get_Point(0).X >= edge.X)
                        {
                            temp_edgeList[0].Type = 1;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 4;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 6;
                                else
                                    temp_edgeList[i].Type = 5;
                            }
                        }
                        else
                        {
                            temp_edgeList[0].Type = 2;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 3;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 5;
                                else
                                    temp_edgeList[i].Type = 6;
                            }
                        }
                    }
                }
                else if (edge.Orientation == 3)
                {
                    if (temp_plgList.Count % 2 == 1)
                    {
                        if (temp_plgList[0].get_Point(0).Y < edge.Y)
                        {
                            temp_edgeList[0].Type = 1;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 3;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 6;
                                else
                                    temp_edgeList[i].Type = 5;
                            }
                        }
                        else
                        {
                            temp_edgeList[0].Type = 2;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 4;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 5;
                                else
                                    temp_edgeList[i].Type = 6;
                            }
                        }
                    }
                    else
                    {
                        if (temp_plgList[0].get_Point(0).Y < edge.Y)
                        {
                            temp_edgeList[0].Type = 1;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 4;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 6;
                                else
                                    temp_edgeList[i].Type = 5;
                            }
                        }
                        else
                        {
                            temp_edgeList[0].Type = 2;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 3;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 5;
                                else
                                    temp_edgeList[i].Type = 6;
                            }
                        }
                    }
                }
                else if (edge.Orientation == 4)
                {
                    if (temp_plgList.Count % 2 == 1)
                    {
                        if (temp_plgList[0].get_Point(0).X < edge.X)
                        {
                            temp_edgeList[0].Type = 1;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 3;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 6;
                                else
                                    temp_edgeList[i].Type = 5;
                            }
                        }
                        else
                        {
                            temp_edgeList[0].Type = 2;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 4;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 5;
                                else
                                    temp_edgeList[i].Type = 6;
                            }
                        }
                    }
                    else
                    {
                        if (temp_plgList[0].get_Point(0).X < edge.X)
                        {
                            temp_edgeList[0].Type = 1;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 4;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 6;
                                else
                                    temp_edgeList[i].Type = 5;
                            }
                        }
                        else
                        {
                            temp_edgeList[0].Type = 2;
                            temp_edgeList[temp_edgeList.Count - 1].Type = 3;
                            for (int i = 1; i < temp_edgeList.Count - 1; i++)
                            {
                                if (i % 2 == 1)
                                    temp_edgeList[i].Type = 5;
                                else
                                    temp_edgeList[i].Type = 6;
                            }
                        }
                    }
                }
                #endregion
                #region
                List<int> tabList = new List<int>();
                int num = temp_plgList.Count;
                for (int i = 0; i < num; i++)
                {
                    double minLength1 = double.MaxValue;
                    double minLength2 = double.MaxValue;
                    int tab1 = 0, tab2 = 0;

                    if (temp_plgList[i].PointCount == 2 || temp_plgList[i].PointCount == 1)
                    {
                        tab1 = temp_edgeList[i].Start;
                        tab2 = temp_edgeList[i].End;
                    }
                    else
                    {
                        IPolygon mbr = new PolygonClass();
                        List<double> terminal = new List<double>();
                        Terminal(temp_plgList[i], terminal);
                        for (int j = 3; j >= 0; j--)
                        {
                            IPoint temp = new PointClass();
                            if (j % 2 == 1)
                            {
                                temp.Y = terminal[j];
                                temp.X = terminal[(j + 1) % 4];
                            }
                            else
                            {
                                temp.X = terminal[j];
                                temp.Y = terminal[(j + 1) % 4];
                            }
                            (mbr as IPointCollection).AddPoint(temp);
                        }

                        int tag = 0;
                        for (int o = 1; o <= 4; o++)
                        {
                            if (temp_edgeList[i].Orientation == o)
                            {
                                tag = (tag + o - 1) % 4;
                                if (temp_edgeList[i].Type == 1)
                                {
                                    tab1 = temp_edgeList[i].Start;
                                    for (int j = 1; j < temp_plgList[i].PointCount; j++)
                                    {
                                        double length = Length((mbr as IPointCollection).get_Point((tag + 1) % 4), temp_plgList[i].get_Point(j));
                                        if (minLength1 > length)
                                        {
                                            minLength1 = length;
                                            tab2 = (temp_edgeList[i].Start + j) % (n - 1);
                                        }
                                    }
                                }
                                if (temp_edgeList[i].Type == 2)
                                {
                                    tab1 = temp_edgeList[i].Start;
                                    for (int j = 1; j < temp_plgList[i].PointCount; j++)
                                    {
                                        double length = Length((mbr as IPointCollection).get_Point((tag + 2) % 4), temp_plgList[i].get_Point(j));
                                        if (minLength1 > length)
                                        {
                                            minLength1 = length;
                                            tab2 = (temp_edgeList[i].Start + j) % (n - 1);
                                        }
                                    }
                                }
                                if (temp_edgeList[i].Type == 3)
                                {
                                    tab2 = temp_edgeList[i].End;
                                    for (int j = 0; j < temp_plgList[i].PointCount - 1; j++)
                                    {
                                        double length = Length((mbr as IPointCollection).get_Point(tag), temp_plgList[i].get_Point(j));
                                        if (minLength1 > length)
                                        {
                                            minLength1 = length;
                                            tab1 = (temp_edgeList[i].Start + j) % (n - 1);
                                        }
                                    }
                                }
                                if (temp_edgeList[i].Type == 4)
                                {
                                    tab2 = temp_edgeList[i].End;
                                    for (int j = 0; j < temp_plgList[i].PointCount - 1; j++)
                                    {
                                        double length = Length((mbr as IPointCollection).get_Point((tag + 3) % 4), temp_plgList[i].get_Point(j));
                                        if (minLength1 > length)
                                        {
                                            minLength1 = length;
                                            tab1 = (temp_edgeList[i].Start + j) % (n - 1);
                                        }
                                    }
                                }
                                if (temp_edgeList[i].Type == 5)
                                {
                                    for (int j = 0; j < temp_plgList[i].PointCount; j++)
                                    {
                                        double length1 = Length((mbr as IPointCollection).get_Point(tag), temp_plgList[i].get_Point(j));
                                        if (minLength1 > length1)
                                        {
                                            minLength1 = length1;
                                            tab1 = (temp_edgeList[i].Start + j) % (n - 1);
                                        }
                                        double length2 = Length((mbr as IPointCollection).get_Point((tag + 1) % 4), temp_plgList[i].get_Point(j));
                                        if (minLength2 > length2)
                                        {
                                            minLength2 = length2;
                                            tab2 = (temp_edgeList[i].Start + j) % (n - 1);
                                        }
                                    }
                                }
                                if (temp_edgeList[i].Type == 6)
                                {
                                    for (int j = 0; j < temp_plgList[i].PointCount; j++)
                                    {
                                        double length1 = Length((mbr as IPointCollection).get_Point((tag + 3) % 4), temp_plgList[i].get_Point(j));
                                        if (minLength1 > length1)
                                        {
                                            minLength1 = length1;
                                            tab1 = (temp_edgeList[i].Start + j) % (n - 1);
                                        }
                                        double length2 = Length((mbr as IPointCollection).get_Point((tag + 2) % 4), temp_plgList[i].get_Point(j));
                                        if (minLength2 > length2)
                                        {
                                            minLength2 = length2;
                                            tab2 = (temp_edgeList[i].Start + j) % (n - 1);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (((tab1 > tab2) && (temp_plgList[i].PointCount != (temp_edgeList[i].End + n - temp_edgeList[i].Start))) || (tab2 - tab1 + 1 > temp_plgList[i].PointCount))
                    {
                        tabList.Add(tab2);
                        tabList.Add(tab1);
                    }
                    else
                    {
                        tabList.Add(tab1);
                        tabList.Add(tab2);
                    }
                }
                #endregion
                #region
                for (int i = 0; i < tabList.Count - 1; i++)
                {
                    Edge mEdge = new Edge();
                    IPointCollection mPtInPlg = new PolylineClass();
                    mEdge.Start = tabList[i];
                    mEdge.End = tabList[i + 1];
                    mEdge.R = edge.R + 1;
                    mEdge.MotherEdge = edge.MotherEdge;
                    if (mEdge.Start <= mEdge.End)
                    {
                        for (int j = tabList[i]; j <= tabList[i + 1]; j++)
                        {
                            if (j >= edge.Start)
                                mPtInPlg.AddPoint(pPtInPlg.get_Point(j - edge.Start));
                            else
                                mPtInPlg.AddPoint(pPtInPlg.get_Point(j + n - 1 - edge.Start));
                        }
                    }
                    else
                    {
                        for (int j = tabList[i]; j <= tabList[i + 1] + n - 1; j++)
                        {
                            if (j >= edge.Start)
                                mPtInPlg.AddPoint(pPtInPlg.get_Point(j - edge.Start));
                            else
                                mPtInPlg.AddPoint(pPtInPlg.get_Point(j + n - 1 - edge.Start));
                        }
                    }
                    temp_plgList.Add(mPtInPlg);
                    temp_edgeList.Add(mEdge);
                }
                for (int i = num; i < temp_plgList.Count; i++)
                {
                    for (int j = 0; j < num; j++)
                    {
                        if ((2 * j + 1) == (i - num + 1))
                        {
                            //if ((tabList[i] > tabList[i + 1]) && (temp_plgList[i].PointCount != (tabList[i + 1] + n - tabList[i])))
                            //    temp_edgeList[i].Orientation = (temp_edgeList[j].Orientation + 2) % 4;
                            //else
                            temp_edgeList[i].Orientation = temp_edgeList[j].Orientation;
                            break;
                        }
                        else if (2 * j == (i - num + 1))
                        {
                            if (temp_edgeList[j].Type == 6)
                            {
                                temp_edgeList[i].Orientation = temp_edgeList[j].Orientation + 1;
                                if (temp_edgeList[i].Orientation > 4)
                                    temp_edgeList[i].Orientation = temp_edgeList[i].Orientation % 4;
                                break;
                            }
                            if (temp_edgeList[j].Type == 5)
                            {
                                temp_edgeList[i].Orientation = temp_edgeList[j].Orientation + 3;
                                if (temp_edgeList[i].Orientation > 4)
                                    temp_edgeList[i].Orientation = temp_edgeList[i].Orientation % 4;
                                break;
                            }
                            if (temp_edgeList[j].Type == 4)
                            {
                                temp_edgeList[i].Orientation = temp_edgeList[j].Orientation + 1;
                                if (temp_edgeList[i].Orientation > 4)
                                    temp_edgeList[i].Orientation = temp_edgeList[i].Orientation % 4;
                                break;
                            }
                            if (temp_edgeList[j].Type == 3)
                            {
                                temp_edgeList[i].Orientation = temp_edgeList[j].Orientation + 3;
                                if (temp_edgeList[i].Orientation > 4)
                                    temp_edgeList[i].Orientation = temp_edgeList[i].Orientation % 4;
                                break;
                            }
                        }
                    }
                }
                #endregion
                temp_plgList.RemoveRange(0, num);
                temp_edgeList.RemoveRange(0, num);
                tabList.Clear();
            }
            #region
            for (int i = temp_plgList.Count - 1; i >= 0; i--)
            {
                edge_List.Insert(1, temp_edgeList[i]);
                PtInEdge.Insert(1, temp_plgList[i]);
            }
            edge_List.RemoveAt(0);
            PtInEdge.RemoveAt(0);
            #endregion
            temp_plgList.Clear();
            temp_edgeList.Clear();
        }

        static private void RegressionLine(IPointCollection pPtInPlg, Edge edge)
        {
            double x = 0, y = 0;
            for (int i = 0; i < pPtInPlg.PointCount; i++)
            {
                x = x + pPtInPlg.get_Point(i).X;
                y = y + pPtInPlg.get_Point(i).Y;
            }
            edge.X = x / pPtInPlg.PointCount;
            edge.Y = y / pPtInPlg.PointCount;
            if (edge.Orientation == 1 || edge.Orientation == 3)
            {
                double sum = 0;
                for (int i = 0; i < pPtInPlg.PointCount; i++)
                    sum = sum + (pPtInPlg.get_Point(i).Y - edge.Y) * (pPtInPlg.get_Point(i).Y - edge.Y);
                sum = sum / pPtInPlg.PointCount;
                edge.SIGMA = System.Math.Sqrt(sum);
            }
            else
            {
                double sum = 0;
                for (int i = 0; i < pPtInPlg.PointCount; i++)
                    sum = sum + (pPtInPlg.get_Point(i).X - edge.X) * (pPtInPlg.get_Point(i).X - edge.X);
                sum = sum / pPtInPlg.PointCount;
                edge.SIGMA = System.Math.Sqrt(sum);
            }
        }

        static private void SmallEdge(IPolygon Plg)
        {
            IPointCollection PtInPlg = Plg as IPointCollection;
            double minLength = double.MaxValue;
            double area = (Plg as IArea).Area;
            if (Length(PtInPlg.get_Point(0), PtInPlg.get_Point(1)) < Length(PtInPlg.get_Point(1), PtInPlg.get_Point(2)))
                minLength = Length(PtInPlg.get_Point(0), PtInPlg.get_Point(1));
            else
                minLength = Length(PtInPlg.get_Point(1), PtInPlg.get_Point(2));
            if (minLength < Threshold)
            {
                IPoint pt = FindCore(Plg);
                double ratio = Threshold / minLength;
                int num = PtInPlg.PointCount;
                for (int i = 0; i < num; i++)
                {
                    IPoint temp = new PointClass();
                    temp.X = pt.X + (PtInPlg.get_Point(i).X - pt.X) * ratio;
                    temp.Y = pt.Y + (PtInPlg.get_Point(i).Y - pt.Y) * ratio;
                    PtInPlg.AddPoint(temp);
                }
                PtInPlg.RemovePoints(0, num);
            }
        }

        static private void AddFeatureToFeatureClass(IFeatureClass Fc, IFeature Fea, IGeometry Geo)
        {
            IFeatureCursor featCur = Fc.Insert(true);
            IFeatureBuffer featBuf = Fc.CreateFeatureBuffer();
            for (int i = 0; i < featBuf.Fields.FieldCount; i++)
            {
                string genFieldName = featBuf.Fields.get_Field(i).Name;
                int index_Field = Fea.Fields.FindField(genFieldName);
                if (index_Field == -1)
                    continue;
                if (featBuf.Fields.get_Field(i).Editable)
                {
                    featBuf.set_Value(i, Fea.get_Value(index_Field));
                }
            }
            featBuf.Shape = Geo;
            featCur.InsertFeature(featBuf);
            featCur.Flush();
            Marshal.ReleaseComObject(featCur);
        }

        static private IFeatureClass CreateNewFeatureClass(string shpName, string dirName, esriGeometryType geoType, ISpatialReference sr)
        {
            IWorkspaceFactory iwSF = new ShapefileWorkspaceFactory();
            IWorkspaceFactoryLockControl iwsflc = iwSF as IWorkspaceFactoryLockControl;
            if (iwsflc.SchemaLockingEnabled)
                iwsflc.DisableSchemaLocking();
            IFeatureWorkspace fWor = iwSF.OpenFromFile(dirName, 0) as IFeatureWorkspace;

            IFields pFields = new FieldsClass();
            IFieldsEdit pFieldsEdit = (IFieldsEdit)pFields;

            IField mField = new FieldClass();
            IFieldEdit mFieldEdit = mField as IFieldEdit;

            mFieldEdit.Name_2 = "Shape";
            mFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
            IGeometryDef pGeoDef = new GeometryDefClass();
            IGeometryDefEdit pGeDEdit = pGeoDef as IGeometryDefEdit;
            pGeDEdit.GeometryType_2 = geoType;
            pGeDEdit.SpatialReference_2 = sr;

            mFieldEdit.GeometryDef_2 = pGeoDef;
            pFieldsEdit.AddField(mField);
            IFeatureClass newFeatClass = fWor.CreateFeatureClass(shpName, pFields, null, null, esriFeatureType.esriFTSimple, "Shape", "");
            return newFeatClass;
        }

        /// <summary>
        /// RRAlgorithm
        /// </summary>
        /// <param name="pfeature"></param>
        /// <returns></returns>
        IPolygon RRAlgorithm(IFeature pfeature)
        {
            IPointCollection pPtInPolygon = (pfeature.ShapeCopy as IPolygon) as IPointCollection;
            IPolygon douglasPlg = new PolygonClass();
            IPointCollection pPtIndouglasPlg = douglasPlg as IPointCollection;
            for (int j = 0; j < pPtInPolygon.PointCount; j++)
                pPtIndouglasPlg.AddPoint(pPtInPolygon.get_Point(j));
            douglasPlg.Generalize(0.2);
            IPolygon ch_Poly = (douglasPlg as ITopologicalOperator).ConvexHull() as IPolygon;
            IPolygon mbr_Poly = MBR(ch_Poly);
            IPointCollection pPtInPlg = mbr_Poly as IPointCollection;
            double angle = System.Math.Acos((pPtInPlg.get_Point(0 + 1).X - pPtInPlg.get_Point(0).X) / Length(pPtInPlg.get_Point(0 + 1), pPtInPlg.get_Point(0)));
            Rotate(mbr_Poly, angle);
            Rotate(douglasPlg, angle);
            FindEgde(mbr_Poly, douglasPlg);
            IPolygon outcome = new PolygonClass();
            if (PtInEdge[0].PointCount + PtInEdge[1].PointCount + PtInEdge[2].PointCount + PtInEdge[3].PointCount > (douglasPlg as IPointCollection).PointCount + 3)
            {
                PtInEdge.Clear();
                edge_List.Clear();
            }
            for (int i = 0; i < PtInEdge.Count; )
                EdgeSplitting(PtInEdge[0], edge_List[0], (douglasPlg as IPointCollection).PointCount);

            #region
            IPointCollection edge1 = new PolylineClass();
            IPointCollection edge2 = new PolylineClass();
            IPointCollection edge3 = new PolylineClass();
            IPointCollection edge4 = new PolylineClass();
            for (int i = 0; i < OutPutList.Count; i++)
            {
                IPoint pt = new PointClass();
                if (OutPutList[i].MotherEdge != OutPutList[(i + 1) % OutPutList.Count].MotherEdge)
                {
                    if (OutPutList[i].Orientation == 1 || OutPutList[i].Orientation == 3)
                    {
                        pt.Y = OutPutList[i].Y;
                        pt.X = OutPutList[(i + 1) % OutPutList.Count].X;
                    }
                    else
                    {
                        pt.X = OutPutList[i].X;
                        pt.Y = OutPutList[(i + 1) % OutPutList.Count].Y;
                    }
                    switch (OutPutList[(i + 1) % OutPutList.Count].MotherEdge)
                    {
                        case 1:
                            edge1.AddPoint(pt);
                            break;
                        case 2:
                            edge2.AddPoint(pt);
                            break;
                        case 3:
                            edge3.AddPoint(pt);
                            break;
                        case 4:
                            edge4.AddPoint(pt);
                            break;
                        default:
                            Console.WriteLine("MotherEdge error!");
                            break;
                    }
                }
            }
            #endregion

            #region
            for (int i = 0; i < OutPutList.Count; i++)
            {
                IPoint pt = new PointClass();
                if (OutPutList[i].Orientation == 1 || OutPutList[i].Orientation == 3)
                {
                    pt.Y = OutPutList[i].Y;
                    pt.X = OutPutList[(i + 1) % OutPutList.Count].X;
                }
                else
                {
                    pt.X = OutPutList[i].X;
                    pt.Y = OutPutList[(i + 1) % OutPutList.Count].Y;
                }
                switch (OutPutList[i].MotherEdge)
                {
                    case 1:
                        edge1.AddPoint(pt);
                        break;
                    case 2:
                        edge2.AddPoint(pt);
                        break;
                    case 3:
                        edge3.AddPoint(pt);
                        break;
                    case 4:
                        edge4.AddPoint(pt);
                        break;
                    default:
                        Console.WriteLine("MotherEdge error!");
                        break;
                }
                (outcome as IPointCollection).AddPoint(pt);
            }
            (outcome as IPointCollection).AddPoint((outcome as IPointCollection).get_Point(0));
            Rotate(outcome, -angle);
            OutPutList.Clear();
            #endregion

            #region
            int iteration = 0; int edges = 0;
            List<int> shortEdge = new List<int>();
            double[] currentSigma = new double[] { Sigma, Sigma, Sigma, Sigma };
            while (IsShortEdgeExit(outcome as IPointCollection))
            {
                if ((outcome as IPointCollection).PointCount == 5)
                {
                    SmallEdge(outcome);
                    break;
                }
                else
                {
                    iteration++;
                    #region
                    if (IsShortEdgeExit(edge1))
                    {
                        currentSigma[0] = 0;
                        if (edge1.PointCount != 2)
                            shortEdge.Add(1);
                    }
                    if (IsShortEdgeExit(edge2))
                    {
                        currentSigma[1] = 0;
                        if (edge2.PointCount != 2)
                            shortEdge.Add(2);
                    }
                    if (IsShortEdgeExit(edge3))
                    {
                        currentSigma[2] = 0;
                        if (edge3.PointCount != 2)
                            shortEdge.Add(3);
                    }
                    if (IsShortEdgeExit(edge4))
                    {
                        currentSigma[3] = 0;
                        if (edge4.PointCount != 2)
                            shortEdge.Add(4);
                    }
                    if (shortEdge.Count == 0)
                        break;
                    #endregion
                    FindEgde(mbr_Poly, douglasPlg);
                    for (int i = 0; i < PtInEdge.Count; )
                    {
                        if (edge_List[0].R == 1)
                        {
                            edges = edges + 1;
                            if (currentSigma[edge_List[0].MotherEdge - 1] != 0)
                                Sigma = currentSigma[edge_List[0].MotherEdge - 1];
                            else
                                Sigma = Threshold / 6;
                            if (shortEdge.Contains(edges))
                            {
                                Sigma = Sigma + 0.1 * iteration;
                                currentSigma[edge_List[0].MotherEdge - 1] = Sigma;
                            }
                        }
                        EdgeSplitting(PtInEdge[0], edge_List[0], (douglasPlg as IPointCollection).PointCount);
                    }
                    (outcome as IPointCollection).RemovePoints(0, (outcome as IPointCollection).PointCount);
                    #region
                    edge1.RemovePoints(0, edge1.PointCount);
                    edge2.RemovePoints(0, edge2.PointCount);
                    edge3.RemovePoints(0, edge3.PointCount);
                    edge4.RemovePoints(0, edge4.PointCount);
                    for (int i = 0; i < OutPutList.Count; i++)
                    {
                        IPoint pt = new PointClass();
                        if (OutPutList[i].MotherEdge != OutPutList[(i + 1) % OutPutList.Count].MotherEdge)
                        {
                            if (OutPutList[i].Orientation == 1 || OutPutList[i].Orientation == 3)
                            {
                                pt.Y = OutPutList[i].Y;
                                pt.X = OutPutList[(i + 1) % OutPutList.Count].X;
                            }
                            else
                            {
                                pt.X = OutPutList[i].X;
                                pt.Y = OutPutList[(i + 1) % OutPutList.Count].Y;
                            }
                            switch (OutPutList[(i + 1) % OutPutList.Count].MotherEdge)
                            {
                                case 1:
                                    edge1.AddPoint(pt);
                                    break;
                                case 2:
                                    edge2.AddPoint(pt);
                                    break;
                                case 3:
                                    edge3.AddPoint(pt);
                                    break;
                                case 4:
                                    edge4.AddPoint(pt);
                                    break;
                                default:
                                    Console.WriteLine("MotherEdge error!");
                                    break;
                            }
                        }
                    }
                    #endregion
                    #region
                    for (int i = 0; i < OutPutList.Count; i++)
                    {
                        IPoint pt = new PointClass();
                        if (OutPutList[i].Orientation == 1 || OutPutList[i].Orientation == 3)
                        {
                            pt.Y = OutPutList[i].Y;
                            pt.X = OutPutList[(i + 1) % OutPutList.Count].X;
                        }
                        else
                        {
                            pt.X = OutPutList[i].X;
                            pt.Y = OutPutList[(i + 1) % OutPutList.Count].Y;
                        }
                        switch (OutPutList[i].MotherEdge)
                        {
                            case 1:
                                edge1.AddPoint(pt);
                                break;
                            case 2:
                                edge2.AddPoint(pt);
                                break;
                            case 3:
                                edge3.AddPoint(pt);
                                break;
                            case 4:
                                edge4.AddPoint(pt);
                                break;
                            default:
                                Console.WriteLine("MotherEdge error!");
                                break;
                        }
                        (outcome as IPointCollection).AddPoint(pt);
                    }
                    (outcome as IPointCollection).AddPoint((outcome as IPointCollection).get_Point(0));
                    Rotate(outcome, -angle);
                    OutPutList.Clear();
                    #endregion
                    edges = 0;
                    shortEdge.Clear();
                }
            }
            #endregion

            Sigma = Threshold / 6;

            return outcome;
        }

        /// <summary>
        /// algorithm application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            #region parameter setting
            //string inPath = OutPath + "\\" + this.comboBox1.Text.ToString() + ".shp";

            if (this.textBox1.Text == "")
            {
                MessageBox.Show("parameter not set, please check!");
                return;
            }
            else
            {
                Threshold = Convert.ToDouble(this.textBox1.Text);
            }

            Sigma = Threshold / 6;

            IFeatureClass selectedFC = pFeatureHandle.GetFeatureClass(pMap, this.comboBox1.Text.ToString());
            ISpatialReference refer = (selectedFC as IGeoDataset).SpatialReference;
            IProjectedCoordinateSystem prj = refer as IProjectedCoordinateSystem;
            if (prj == null)
            {
                MessageBox.Show("Not ProjectedCoordinateSystem, please check!");
                return;
            }
            #endregion

            #region algorithm application
            string shpName = selectedFC.AliasName;
            //string currentDirectory = System.IO.Path.GetDirectoryName(inPath);
            string shpNm = shpName + "_RecursiveApproach" + "_" + Convert.ToString(Threshold);
            string shpPath = System.IO.Path.Combine(OutPath, shpNm);
            string[] shpFile = System.IO.Directory.GetFiles(OutPath, shpNm + ".*");
            for (int i = 0; i < shpFile.Length; i++)
                File.Delete(shpFile[i]);
            IFeatureClass Recursive = CreateNewFeatureClass(shpNm, OutPath, esriGeometryType.esriGeometryPolygon, refer);

            IWorkspaceFactory workspaceFactory = new ShapefileWorkspaceFactoryClass();
            IWorkspace workspace = workspaceFactory.OpenFromFile(System.IO.Path.GetDirectoryName(shpPath), 0);
            IWorkspaceEdit workspaceEdit = workspace as IWorkspaceEdit;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            IFeatureCursor searchCursor = selectedFC.Search(null, false);
            int runCount = 0;
            int allCount = selectedFC.FeatureCount(null);
            this.progressBar1.Maximum = allCount;
            IFeature currentFeature = null;
            currentFeature = searchCursor.NextFeature();
            while (currentFeature != null)
            {
                if (JudgeGeometryNormal(currentFeature.ShapeCopy) != -1)
                {
                    currentFeature = searchCursor.NextFeature();
                    continue;
                }

                IPolygon outcome = this.RRAlgorithm(currentFeature); 
                Sigma = Threshold / 6;
                AddFeatureToFeatureClass(Recursive, currentFeature, outcome);
                currentFeature = searchCursor.NextFeature();
                runCount++;
                Console.WriteLine(runCount + " / " + allCount);
                this.progressBar1.Value = runCount;
            }
            Marshal.FinalReleaseComObject(searchCursor);

            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
            //Console.ReadKey();
            //Do not make any call to ArcObjects after ShutDownApplication()
            #endregion

            MessageBox.Show("Done!");
        }
    }
}
