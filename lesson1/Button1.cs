using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;

namespace ArcMapAddin1
{
    public class Button1 : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        private static List<double> pntlst = new List<double> { -20037508.34, -20037508.34, 20037508.34, 20037508.34, -20037508.34 };
        private static List<IPoint> pntclassList = new List<IPoint>(pntlst.Count);
        static Button1()
        {
            IPoint p1 = null;
            for (int i = 0; i < pntlst.Count - 1; ++i)
            {
                p1 = new PointClass();
                p1.X = pntlst[i];
                p1.Y = pntlst[i + 1];
                pntclassList.Add(p1);
            }
        }
        private IMap map;
        private ISimpleMarkerSymbol marker = new SimpleMarkerSymbolClass();
        private IMxDocument mxdoc = null;
        private bool hasClicked = false;
        public Button1()
        {
            mxdoc = ArcMap.Application.Document as IMxDocument;
            map = mxdoc.FocusMap as IMap;
            IActiveViewEvents_Event evt = map as IActiveViewEvents_Event;
            evt.AfterDraw += (IDisplay disp, esriViewDrawPhase phase) =>
                {
                    if (hasClicked && esriViewDrawPhase.esriViewForeground == phase)
                    {
                        disp.StartDrawing(disp.hDC, Convert.ToInt16(esriScreenCache.esriNoScreenCache));
                        disp.SetSymbol(marker as ISymbol);
                        foreach (IPoint pc in pntclassList)
                            disp.DrawPoint(pc);
                        disp.FinishDrawing();
                    }
                };

            IRgbColor color = new RgbColorClass();
            color.Red = 255;
            color.Blue = 0;
            color.Green = 0;
            marker.Color = color;
        }
        protected override void OnClick()
        {
            //
            //  TODO: Sample code showing how to access button host
            //
            ArcMap.Application.CurrentTool = null;
            if (false == hasClicked)
            {
                hasClicked = true;
                IActiveView view = mxdoc.ActiveView;
                view.Refresh();
            }
        }
        protected override void OnUpdate()
        {
            Enabled = ArcMap.Application != null;
        }
    }

}
