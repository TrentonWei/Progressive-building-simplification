using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;

namespace PrDispalce.PublicUtil
{
    class ToolsForTS
    {
        /// <summary>
        /// 获得旋转后的多边形
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <param name="Orientation"></param>
        /// <returns></returns>
        public IPolygon GetRotatedPolygon(IPolygon pPolygon, double Orientation)
        {
            IArea pArea = pPolygon as IArea;
            IPoint CenterPoint = pArea.Centroid;
            ITransform2D pTransform2D = pPolygon as ITransform2D;
            pTransform2D.Rotate(CenterPoint, Orientation);
            return pTransform2D as IPolygon;
        }

        /// <summary>
        /// 获得平移后的多边形
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <param name="pPoint"></param>
        /// <returns></returns>
        public IPolygon GetPannedPolygon(IPolygon pPolygon, IPoint pPoint)
        {
            IArea pArea = pPolygon as IArea;
            IPoint CenterPoint = pArea.Centroid;

            double Dx = pPoint.X - CenterPoint.X;
            double Dy = pPoint.Y - CenterPoint.Y;

            ITransform2D pTransform2D = pPolygon as ITransform2D;
            pTransform2D.Move(Dx, Dy);
            return pTransform2D as IPolygon;
        }

        /// <summary>
        /// 获得放大后的多边形
        /// </summary>
        /// <param name="pPolygon"></param>
        /// <param name="EnlargeRate"></param>
        /// <returns></returns>
        public IPolygon GetEnlargedPolygon(IPolygon pPolygon, double EnlargeRate)
        {
            IArea pArea = pPolygon as IArea;
            IPoint CenterPoint = pArea.Centroid;

            ITransform2D pTransform2D = pPolygon as ITransform2D;
            pTransform2D.Scale(CenterPoint, EnlargeRate, EnlargeRate);
            return pTransform2D as IPolygon;
        }
    }
}
