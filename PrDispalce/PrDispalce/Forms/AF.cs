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
    public partial class AF : Form
    {
        public AF(AxMapControl axMapControl)
        {
            InitializeComponent();
            this.pMap = axMapControl.Map;
            this.pMapControl = axMapControl;
        }

        #region Parameters
        AxMapControl pMapControl;
        IMap pMap;
        string OutPath;
        static double Sv;
        PrDispalce.PublicUtil.FeatureHandle pFeatureHandle = new PublicUtil.FeatureHandle();
        #endregion

        /// <summary>
        /// initialize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AF_Load(object sender, EventArgs e)
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
            this.comboBox2.Text = OutPath;
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

        static private List<IPoint> ReList(IPolygon pPlg)
        {
            List<IPoint> pPtInPlg = new List<IPoint>();
            IPointCollection pPtInPolygon = pPlg as IPointCollection;

            IPolygon douglasPlg = new PolygonClass();
            IPointCollection pPtIndouglasPlg = douglasPlg as IPointCollection;
            for (int i = 0; i < pPtInPolygon.PointCount; i++)
                pPtIndouglasPlg.AddPoint(pPtInPolygon.get_Point(i));
            douglasPlg.Generalize(0.2);
            double maxLength = 0;
            int flag = 0;
            for (int i = 0; i < pPtIndouglasPlg.PointCount - 1; i++)
            {
                double length = Length(pPtIndouglasPlg.get_Point(i), pPtIndouglasPlg.get_Point(i + 1));
                if (maxLength < length)
                {
                    maxLength = length;
                    flag = i;
                }
            }
            for (int i = flag; i < pPtIndouglasPlg.PointCount - 1; i++)
                pPtInPlg.Add(pPtIndouglasPlg.get_Point(i));
            for (int i = 0; i < flag; i++)
                pPtInPlg.Add(pPtIndouglasPlg.get_Point(i));
            pPtInPlg.Add(pPtIndouglasPlg.get_Point(flag));
            return pPtInPlg;
        }

        static private int TypeJudge(IPoint pPt1, IPoint pPt2, IPoint pPt3, IPoint pPt4)
        {
            int type = 0;
            double dx, dy, dx1, dx2, dy1, dy2, same, S12, S34;
            dx = pPt3.X - pPt2.X;
            dy = pPt3.Y - pPt2.Y;
            dx1 = pPt1.X - pPt2.X;
            dx2 = pPt4.X - pPt3.X;
            dy1 = pPt1.Y - pPt2.Y;
            dy2 = pPt4.Y - pPt3.Y;
            S12 = Length(pPt1, pPt2);
            S34 = Length(pPt3, pPt4);
            same = (dx * dy1 - dy * dx1) * (dx * dy2 - dy * dx2);
            if (same < 0)
                type = 1;
            if (same > 0)
            {
                if ((S12 - S34) > (Sv / 5))
                    type = 2;
                else if ((S34 - S12) > (Sv / 5))
                    type = 3;
                else
                    type = 4;
            }
            return type;
        }

        static private void SharpErase(IPoint pPt1, IPoint pPt2, IPoint pPt3, IPoint pPt4, int type)
        {
            double angle = GetAngle(pPt1, pPt2, pPt3, pPt4);
            double S1 = Distance(pPt2, pPt3, pPt4);
            double S2 = Distance(pPt3, pPt1, pPt2);
            if (type == 1 && (S1 < Sv || S2 < Sv))
            {
                if (angle < (System.Math.PI / 18) || angle > (17 * System.Math.PI / 18))
                {
                    IPoint vertical = new PointClass();
                    IPoint cross = new PointClass();
                    CrossVertical(pPt1, pPt2, pPt3, pPt4, vertical, cross);
                    if (Length(pPt1, pPt2) < Length(pPt3, pPt4))
                    {
                        pPt2.X = cross.X;
                        pPt2.Y = cross.Y;
                        pPt3.X = vertical.X;
                        pPt3.Y = vertical.Y;
                    }
                    else
                    {
                        pPt3.X = cross.X;
                        pPt3.Y = cross.Y;
                        pPt2.X = vertical.X;
                        pPt2.Y = vertical.Y;
                    }
                }
                else if (Distance(pPt2, pPt1, pPt3) < (Length(pPt1, pPt3) / 15))
                {
                    pPt2.X = pPt3.X;
                    pPt2.Y = pPt3.Y;
                }
                else if (Distance(pPt3, pPt2, pPt4) < (Length(pPt2, pPt4) / 15))
                {
                    pPt3.X = pPt2.X;
                    pPt3.Y = pPt2.Y;
                }
                else
                {
                    IPoint replace = new PointClass();
                    double S1d = Distance(pPt1, pPt2, pPt3);
                    double S4c = Distance(pPt4, pPt2, pPt3);
                    replace.X = pPt2.X + (pPt3.X - pPt2.X) * S4c / (S1d + S4c);
                    replace.Y = pPt2.Y + (pPt3.Y - pPt2.Y) * S4c / (S1d + S4c);
                    pPt2.X = replace.X;
                    pPt2.Y = replace.Y;
                    pPt3.X = replace.X;
                    pPt3.Y = replace.Y;
                }
            }
            if ((type == 2 || type == 3 || type == 4) && (S1 < Sv && S2 < Sv))
            {
                if (angle < (System.Math.PI / 4))
                {
                    IPoint vertical = new PointClass();
                    if (Length(pPt1, pPt2) < Length(pPt3, pPt4))
                    {
                        double A = pPt4.Y - pPt3.Y;
                        double B = pPt3.X - pPt4.X;
                        double C = pPt4.X * pPt3.Y - pPt4.Y * pPt3.X;
                        vertical.X = (B * B * pPt2.X - A * B * pPt2.Y - A * C) / (A * A + B * B);
                        vertical.Y = (A * A * pPt2.Y - A * B * pPt2.X - B * C) / (A * A + B * B);
                        pPt3.X = vertical.X;
                        pPt3.Y = vertical.Y;
                    }
                    else
                    {
                        double A = pPt2.Y - pPt1.Y;
                        double B = pPt1.X - pPt2.X;
                        double C = pPt2.X * pPt1.Y - pPt2.Y * pPt1.X;
                        vertical.X = (B * B * pPt3.X - A * B * pPt3.Y - A * C) / (A * A + B * B);
                        vertical.Y = (A * A * pPt3.Y - A * B * pPt3.X - B * C) / (A * A + B * B);
                        pPt2.X = vertical.X;
                        pPt2.Y = vertical.Y;
                    }
                }
                else
                {
                    IPoint cross = new PointClass();
                    double A1 = pPt2.Y - pPt1.Y;
                    double B1 = pPt1.X - pPt2.X;
                    double C1 = pPt2.X * pPt1.Y - pPt2.Y * pPt1.X;
                    double A2 = pPt4.Y - pPt3.Y;
                    double B2 = pPt3.X - pPt4.X;
                    double C2 = pPt4.X * pPt3.Y - pPt4.Y * pPt3.X;
                    cross.X = (B1 * C2 - B2 * C1) / (A1 * B2 - A2 * B1);
                    cross.Y = (C1 * A2 - A1 * C2) / (A1 * B2 - A2 * B1);
                    pPt2.X = cross.X;
                    pPt2.Y = cross.Y;
                    pPt3.X = cross.X;
                    pPt3.Y = cross.Y;
                }
            }
        }

        static private void FourPointMethed(IPoint pPt1, IPoint pPt2, IPoint pPt3, IPoint pPt4, int type)
        {
            double S12 = Length(pPt1, pPt2);
            double S34 = Length(pPt3, pPt4);
            double S23 = Length(pPt2, pPt3);
            if (type == 1)
            {
                double q1 = S34 / (S12 + S34);
                pPt1.X = pPt1.X + q1 * (pPt3.X - pPt2.X);
                pPt1.Y = pPt1.Y + q1 * (pPt3.Y - pPt2.Y);
                double q2 = S12 / (S12 + S34);
                pPt4.X = pPt4.X + q2 * (pPt2.X - pPt3.X);
                pPt4.Y = pPt4.Y + q2 * (pPt2.Y - pPt3.Y);
                pPt2.X = pPt1.X;
                pPt2.Y = pPt1.Y;
                pPt3.X = pPt1.X;
                pPt3.Y = pPt1.Y;
            }
            if (type == 2)
            {
                if ((S23 * S34) < (Sv * Sv))
                {
                    pPt4.X = pPt4.X + pPt2.X - pPt3.X;
                    pPt4.Y = pPt4.Y + pPt2.Y - pPt3.Y;
                    pPt2.X = pPt1.X;
                    pPt2.Y = pPt1.Y;
                    pPt3.X = pPt1.X;
                    pPt3.Y = pPt1.Y;
                }
                else
                {
                    double Sbc = S23 * S34 / Sv;
                    IPoint l34 = new PointClass();
                    l34.X = pPt3.X - pPt4.X;
                    l34.Y = pPt3.Y - pPt4.Y;
                    pPt4.X = pPt4.X + (pPt3.X - pPt2.X) * (Sv - S23) / S23;
                    pPt4.Y = pPt4.Y + (pPt3.Y - pPt2.Y) * (Sv - S23) / S23;
                    pPt3.X = pPt4.X + l34.X * Sbc / S34;
                    pPt3.Y = pPt4.Y + l34.Y * Sbc / S34;
                    pPt2.X = pPt2.X + (pPt1.X - pPt2.X) * (S34 - Sbc) / S12;
                    pPt2.Y = pPt2.Y + (pPt1.Y - pPt2.Y) * (S34 - Sbc) / S12;
                }
            }
            if (type == 3)
            {
                if ((S23 * S12) < (Sv * Sv))
                {
                    pPt1.X = pPt1.X + pPt3.X - pPt2.X;
                    pPt1.Y = pPt1.Y + pPt3.Y - pPt2.Y;
                    pPt2.X = pPt1.X;
                    pPt2.Y = pPt1.Y;
                    pPt3.X = pPt1.X;
                    pPt3.Y = pPt1.Y;
                }
                else
                {
                    double Sbc = S23 * S12 / Sv;
                    IPoint l12 = new PointClass();
                    l12.X = pPt2.X - pPt1.X;
                    l12.Y = pPt2.Y - pPt1.Y;
                    pPt1.X = pPt1.X + (pPt2.X - pPt3.X) * ((Sv - S23) / S23);
                    pPt1.Y = pPt1.Y + (pPt2.Y - pPt3.Y) * ((Sv - S23) / S23);
                    pPt2.X = pPt1.X + l12.X * (Sbc / S12);
                    pPt2.Y = pPt1.Y + l12.Y * (Sbc / S12);
                    pPt3.X = pPt3.X + (pPt4.X - pPt3.X) * ((S12 - Sbc) / S34);
                    pPt3.Y = pPt3.Y + (pPt4.Y - pPt3.Y) * ((S12 - Sbc) / S34);
                }
            }
            if (type == 4)
            {
                if ((S23 * S34) < (Sv * Sv) || (S23 * S12) < (Sv * Sv))
                {
                    pPt2.X = pPt1.X;
                    pPt2.Y = pPt1.Y;
                    pPt3.X = pPt1.X;
                    pPt3.Y = pPt1.Y;
                }
                else
                {
                    double Sab = S23 * S34 / Sv;
                    IPoint l12 = new PointClass();
                    l12.X = pPt2.X - pPt1.X;
                    l12.Y = pPt2.Y - pPt1.Y;
                    IPoint l34 = new PointClass();
                    l34.X = pPt3.X - pPt4.X;
                    l34.Y = pPt3.Y - pPt4.Y;
                    pPt1.X = pPt1.X + (pPt2.X - pPt3.X) * (Sv - S23) / (2 * S23);
                    pPt1.Y = pPt1.Y + (pPt2.Y - pPt3.Y) * (Sv - S23) / (2 * S23);
                    pPt4.X = pPt4.X + (pPt3.X - pPt2.X) * (Sv - S23) / (2 * S23);
                    pPt4.Y = pPt4.Y + (pPt3.Y - pPt2.Y) * (Sv - S23) / (2 * S23);
                    pPt2.X = pPt1.X + l12.X * Sab / S12;
                    pPt2.Y = pPt1.Y + l12.Y * Sab / S12;
                    pPt3.X = pPt4.X + l34.X * Sab / S34;
                    pPt3.Y = pPt4.Y + l34.Y * Sab / S34;
                }
            }
        }

        static private double GetAngle(IPoint pPt1, IPoint pPt2, IPoint pPt3, IPoint pPt4)
        {
            double angle = 0;
            IPoint temp = new PointClass();
            temp.X = pPt4.X - pPt3.X + pPt2.X;
            temp.Y = pPt4.Y - pPt3.Y + pPt2.Y;
            double b = Length(pPt2, temp);
            double c = Length(pPt1, pPt2);
            double a = Length(pPt1, temp);
            angle = System.Math.Acos(System.Math.Round((b * b + c * c - a * a) / (2 * b * c), 8));
            return angle;
        }

        static private double Distance(IPoint pPt1, IPoint pPt2, IPoint pPt3)
        {
            double distance;
            double A = pPt3.Y - pPt2.Y;
            double B = pPt2.X - pPt3.X;
            double C = pPt3.X * pPt2.Y - pPt3.Y * pPt2.X;
            distance = System.Math.Abs((A * pPt1.X + B * pPt1.Y + C) / System.Math.Sqrt(A * A + B * B));
            return distance;
        }

        static private void CrossVertical(IPoint pPt1, IPoint pPt2, IPoint pPt3, IPoint pPt4, IPoint vertical, IPoint cross)
        {
            if (Length(pPt1, pPt2) < Length(pPt3, pPt4))
            {
                double A1 = pPt4.Y - pPt3.Y;
                double B1 = pPt3.X - pPt4.X;
                double C1 = pPt4.X * pPt3.Y - pPt4.Y * pPt3.X;
                vertical.X = (B1 * B1 * (pPt2.X + pPt3.X) / 2 - A1 * B1 * (pPt2.Y + pPt3.Y) / 2 - A1 * C1) / (A1 * A1 + B1 * B1);
                vertical.Y = (A1 * A1 * (pPt2.Y + pPt3.Y) / 2 - A1 * B1 * (pPt2.X + pPt3.X) / 2 - B1 * C1) / (A1 * A1 + B1 * B1);
                double A2 = (pPt2.Y + pPt3.Y) / 2 - vertical.Y;
                double B2 = vertical.X - (pPt2.X + pPt3.X) / 2;
                double C2 = (pPt2.X + pPt3.X) / 2 * vertical.Y - (pPt2.Y + pPt3.Y) / 2 * vertical.X;
                double A3 = pPt2.Y - pPt1.Y;
                double B3 = pPt1.X - pPt2.X;
                double C3 = pPt2.X * pPt1.Y - pPt2.Y * pPt1.X;
                cross.X = (B3 * C2 - B2 * C3) / (A3 * B2 - A2 * B3);
                cross.Y = (C3 * A2 - A3 * C2) / (A3 * B2 - A2 * B3);
            }
            else
            {
                double A1 = pPt2.Y - pPt1.Y;
                double B1 = pPt1.X - pPt2.X;
                double C1 = pPt2.X * pPt1.Y - pPt2.Y * pPt1.X;
                vertical.X = (B1 * B1 * (pPt2.X + pPt3.X) / 2 - A1 * B1 * (pPt2.Y + pPt3.Y) / 2 - A1 * C1) / (A1 * A1 + B1 * B1);
                vertical.Y = (A1 * A1 * (pPt2.Y + pPt3.Y) / 2 - A1 * B1 * (pPt2.X + pPt3.X) / 2 - B1 * C1) / (A1 * A1 + B1 * B1);
                double A2 = (pPt2.Y + pPt3.Y) / 2 - vertical.Y;
                double B2 = vertical.X - (pPt2.X + pPt3.X) / 2;
                double C2 = (pPt2.X + pPt3.X) / 2 * vertical.Y - (pPt2.Y + pPt3.Y) / 2 * vertical.X;
                double A3 = pPt4.Y - pPt3.Y;
                double B3 = pPt3.X - pPt4.X;
                double C3 = pPt4.X * pPt3.Y - pPt4.Y * pPt3.X;
                cross.X = (B3 * C2 - B2 * C3) / (A3 * B2 - A2 * B3);
                cross.Y = (C3 * A2 - A3 * C2) / (A3 * B2 - A2 * B3);
            }
        }

        static private double Length(IPoint pPt1, IPoint pPt2)
        {
            double length = System.Math.Sqrt((pPt2.X - pPt1.X) * (pPt2.X - pPt1.X) + (pPt2.Y - pPt1.Y) * (pPt2.Y - pPt1.Y));
            return length;
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
        /// AF algorithm
        /// </summary>
        /// <param name="pfeature"></param>
        /// <returns></returns>
        private IPointCollection AFAlgorithm(IFeature pfeature)
        {
            IPointCollection pPtPlg = new PolygonClass();

            #region Main algorithm
            List<IPoint> longFirst = ReList(pfeature.ShapeCopy as IPolygon);
            if (longFirst.Count < 5)
            {
                for (int j = 0; j < longFirst.Count - 1; j++)
                    pPtPlg.AddPoint(longFirst[j]);
                pPtPlg.AddPoint(longFirst[0]);
            }
            else
            {
                for (int j = 0; j < longFirst.Count - 1; j++)
                {
                    int fpType = TypeJudge(longFirst[j % (longFirst.Count - 1)], longFirst[(j + 1) % (longFirst.Count - 1)], longFirst[(j + 2) % (longFirst.Count - 1)], longFirst[(j + 3) % (longFirst.Count - 1)]);
                    if (fpType == 0)
                        continue;
                    SharpErase(longFirst[j % (longFirst.Count - 1)], longFirst[(j + 1) % (longFirst.Count - 1)], longFirst[(j + 2) % (longFirst.Count - 1)], longFirst[(j + 3) % (longFirst.Count - 1)], fpType);
                    if ((longFirst[(j + 1) % (longFirst.Count - 1)].X == longFirst[(j + 2) % (longFirst.Count - 1)].X) && (longFirst[(j + 1) % (longFirst.Count - 1)].Y == longFirst[(j + 2) % (longFirst.Count - 1)].Y))
                    {
                        longFirst.RemoveAt((j + 2) % (longFirst.Count - 1));
                        j--;
                        continue;
                    }
                    else
                    {
                        double S23 = Length(longFirst[(j + 1) % (longFirst.Count - 1)], longFirst[(j + 2) % (longFirst.Count - 1)]);
                        double d = System.Math.Round(Sv - S23, 4);
                        S23 = Sv - d;
                        if (S23 < Sv)
                        {
                            FourPointMethed(longFirst[j % (longFirst.Count - 1)], longFirst[(j + 1) % (longFirst.Count - 1)], longFirst[(j + 2) % (longFirst.Count - 1)], longFirst[(j + 3) % (longFirst.Count - 1)], fpType);
                            if ((longFirst[(j + 1) % (longFirst.Count - 1)].X == longFirst[(j + 2) % (longFirst.Count - 1)].X) && (longFirst[(j + 1) % (longFirst.Count - 1)].Y == longFirst[(j + 2) % (longFirst.Count - 1)].Y))
                            {
                                if (j + 2 == (longFirst.Count - 1))
                                {
                                    longFirst.RemoveAt((j + 2) % (longFirst.Count - 1));
                                    longFirst.RemoveAt(j % (longFirst.Count - 1));
                                }
                                else if (j + 1 == (longFirst.Count - 1))
                                {
                                    longFirst.RemoveAt((j + 1) % (longFirst.Count - 1));
                                    longFirst.RemoveAt(j % (longFirst.Count - 1));
                                }
                                else
                                {
                                    longFirst.RemoveAt((j + 1) % (longFirst.Count - 1));
                                    longFirst.RemoveAt((j + 1) % (longFirst.Count - 1));
                                }
                                if (fpType == 1)
                                {
                                    if (j > 1)
                                        j = j - 3;
                                    else if (j == 1)
                                        j = j - 2;
                                    else
                                        j--;
                                    continue;
                                }
                                else
                                {
                                    if (j == 0)
                                    {
                                        j--;
                                        continue;
                                    }
                                    else
                                    {
                                        j = j - 2;
                                        continue;
                                    }
                                }
                            }
                            else
                            {
                                if (fpType == 3 || fpType == 4)
                                {
                                    if (j > 1)
                                        j = j - 3;
                                    else if (j == 1)
                                        j = j - 2;
                                    else
                                        j--;
                                }
                                if (fpType == 2)
                                {
                                    if (j > 0)
                                        j = j - 2;
                                    else
                                        j--;
                                }
                                //j++;
                                continue;
                            }
                        }
                    }
                }
                for (int j = 0; j < longFirst.Count - 1; j++)
                    pPtPlg.AddPoint(longFirst[j]);
                pPtPlg.AddPoint(longFirst[0]);
            }
            #endregion

            return pPtPlg;
        }

        /// <summary>
        /// algorithm application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
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
                Sv = Convert.ToDouble(this.textBox1.Text);
            }

            IFeatureClass selectedFC = pFeatureHandle.GetFeatureClass(pMap, this.comboBox1.Text.ToString());
            ISpatialReference refer = (selectedFC as IGeoDataset).SpatialReference;
            IProjectedCoordinateSystem prj = refer as IProjectedCoordinateSystem;
            if (prj == null)
            {
                MessageBox.Show("Not ProjectedCoordinateSystem, please check!");
                return;
            }
            #endregion

            string shpName = selectedFC.AliasName;
            //string currentDirectory = System.IO.Path.GetDirectoryName(inPath);
            string shpNm = shpName + "_AFPM" + "_" + Convert.ToString(Sv);
            string shpPath = System.IO.Path.Combine(OutPath, shpNm);
            string[] shpFile = System.IO.Directory.GetFiles(OutPath, shpNm + ".*");
            for (int i = 0; i < shpFile.Length; i++)
                File.Delete(shpFile[i]);
            IFeatureClass afpm = CreateNewFeatureClass(shpNm, OutPath, esriGeometryType.esriGeometryPolygon, refer);

            #region algorithm application
            IWorkspaceFactory workspaceFactory = new ShapefileWorkspaceFactoryClass();
            IWorkspace workspace = workspaceFactory.OpenFromFile(System.IO.Path.GetDirectoryName(shpPath), 0);
            IWorkspaceEdit workspaceEdit = workspace as IWorkspaceEdit;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

           
            IFeatureCursor searchCursor = selectedFC.Search(null, false);
            int count = 0;
            int allCount = selectedFC.FeatureCount(null);
            this.progressBar1.Maximum = allCount;
            IFeature currentFeature = null;
            while ((currentFeature = searchCursor.NextFeature()) != null)
            {
                count++;

                if (JudgeGeometryNormal(currentFeature.ShapeCopy) != -1)
                    continue;

                IPointCollection pPtPlg = this.AFAlgorithm(currentFeature);

                if (pPtPlg.PointCount >= 4)
                    AddFeatureToFeatureClass(afpm, currentFeature, pPtPlg as IPolygon);
                else
                    AddFeatureToFeatureClass(afpm, currentFeature, currentFeature.ShapeCopy);
                Console.WriteLine(count + " / " + allCount);
                this.progressBar1.Value = count;
            }
            #endregion

            Marshal.FinalReleaseComObject(searchCursor);
            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);

            MessageBox.Show("Done!");
        }
    }
}
