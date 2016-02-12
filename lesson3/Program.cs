using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ESRI.ArcGIS;
//using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace AOvsAP
{
    sealed class DownstreamNode
    {
        private int incomingEdgeId;
        
        public int IncomingEdgeId
        {
            get { return incomingEdgeId; }
            //set { incomingEdgeId = value; }
        }
        private int id;

        public int Id
        {
            get { return id; }
            //set { id = value; }
        }

        private IEnumerable<double> velocities = null;

        public IEnumerable<double> Velocities
        {
            get { return velocities; }
            //set { velocities = value; }
        }
        public DownstreamNode(int id, int eid, IEnumerable<double> vecs)
        {
            this.id = id;
            this.incomingEdgeId = eid;
            this.velocities = vecs;
        }

    }
    sealed class Vertex
    {
        private int id;

        public int Id
        {
            get { return id; }
            //set { id = value; }
        }
        private int incomingStreamCount;

        public int IncomingStreamCount
        {
            get { return incomingStreamCount; }
            //set { incomingStreamCount = value; }
        }
        List<DownstreamNode> downstreams;

        internal List<DownstreamNode> Downstreams
        {
            get { return downstreams; }
            //set { downstreams = value; }
        }
        public Vertex(int id, int incomingCnt, List<DownstreamNode> ds)
        {
            this.id = id;
            this.incomingStreamCount = incomingCnt;
            this.downstreams = ds;
        }
    }
    class Program
    {
        private static LicenseInitializer m_AOLicenseInitializer = new AOvsAP.LicenseInitializer();
        private const double Tolerence = 1e-8;
        static void ReleaseCOMObj(object comobj)
        {
            int refCount = 0;
            do
            {
                refCount = System.Runtime.InteropServices.Marshal.ReleaseComObject(comobj);
            } while (refCount > 0);
        }
        static bool isSamePoint(IPoint p1, IPoint p2)
        {
            return Math.Abs(p1.X - p2.X) < Tolerence && Math.Abs(p1.Y - p2.Y) < Tolerence;
        }
        [STAThread()]
        static void Main(string[] args)
        {
            //ESRI License Initializer generated code.
            m_AOLicenseInitializer.InitializeApplication(new esriLicenseProductCode[] { esriLicenseProductCode.esriLicenseProductCodeAdvanced },
            new esriLicenseExtensionCode[] { });

            Dictionary<string, Vertex> nodeDict = new Dictionary<string, Vertex>();
            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspace workspace = workspaceFactory.OpenFromFile(@"C:\Users\hellocomrade\NHDPlusV21_GL_04.gdb", 0);
            IFeatureWorkspace iFtrWs = workspace as IFeatureWorkspace;
            if(null != iFtrWs)
            {
                IFeatureClass fcLine = iFtrWs.OpenFeatureClass("NHD_Flowline");
                IFeatureClass fcPoint = iFtrWs.OpenFeatureClass("Hydro_Net_Junctions");
                System.Diagnostics.Debug.Assert(null != fcLine && null != fcPoint);

                var fldLst = new List<string>{"OBJECTID","FlowDir","FTYPE","V0001E_MA","V0001E_MM","V0001E_01","V0001E_02","V0001E_03","V0001E_04","V0001E_05","V0001E_06","V0001E_07","V0001E_08","V0001E_09","V0001E_10","V0001E_11","V0001E_12"};
                Dictionary<string,int> fldDict = new Dictionary<string,int>();
                fldLst.ForEach(fldName => fldDict.Add(fldName, fcLine.FindField(fldName)));

                int lineFtrId = -1;
                //Find field index for oid in Hydro_Net_Junctions feature class
                int pntFtrIdIdx = fcPoint.FindField("OBJECTID");
                string pntFtrId = null;

                /*
                 * It has been observed that the most time consuming part of this script is on spatial query.
                 * We could take the same approach we had on Arcpy through fcLine.Search() with a spatial filter.
                 * However, it will make us have the same granularity as using arcpy. 
                 * Instead, we took a different route here by using IFeatureIndex and IIndexQuery2
                 */ 
                IGeoDataset geoLineDS = (IGeoDataset)fcLine;
                ISpatialReference srLine = geoLineDS.SpatialReference;
                IFeatureIndex lineIdx = new FeatureIndexClass();
                lineIdx.FeatureClass = fcLine;
                lineIdx.Index(null, geoLineDS.Extent);
                IIndexQuery2 queryLineByIdx = lineIdx as IIndexQuery2;

                IFeatureIndex pointIdx = new FeatureIndexClass();
                pointIdx.FeatureClass = fcPoint;
                pointIdx.Index(null, ((IGeoDataset)fcPoint).Extent);
                IIndexQuery2 queryPointByIdx = pointIdx as IIndexQuery2;

                //Get a cursor on Hydro_Net_Junctions as the iterator
                IFeatureCursor pntCur = fcPoint.Search(null, false);
                IFeature pnt = pntCur.NextFeature();
                IFeature line = null;
                IFeature otherPnt = null;
                List<string> requiredTypes = new List<string>{"StreamRiver","ArtificialPath"};
                /*
                 * ITopologicalOpeartor is good for two geometries comparing with each other. It doesn't fit
                 * very well for our situation, which check the geometric relationships between a geometry
                 * against a feature class that may have more than 100K geometries.
                */ 
                //ITopologicalOperator optor = null;

                /*
                 * It's a shame that we have to reference to a Server API in order to calcuate Geodesic
                 * distances for a polyline with lon/lat as coordinates.
                 * 
                 * We could do Haversine ourselves, but we have to believe ESRI could definitely does a 
                 * better job there, well, hard to figure out how to get this simple task done in an intutive
                 * way though...
                 */ 
                IGeometryServer2 geoOperator = new GeometryServerClass();
                
                IPolyline polyLine = null;
                List<DownstreamNode> dsLst = null;
                int[] lineIds = null;
                int[] pntIds = null;
                object idobjs;
                object idobjs1;
                PolylineArray tmpArr = null;
                IDoubleArray lengths = null;
                double lineLen = 0.0;
                double v = 0.0;
                ILinearUnit unit = new LinearUnitClass();
                var range = Enumerable.Range(3, 14);
                int count = 0;
                int incoming = 0;
                while(null != pnt)
                {
                    //get ftr id of the current vertex
                    pntFtrId = pnt.get_Value(pntFtrIdIdx).ToString();
                    //optor = pnt.Shape as ITopologicalOperator;
                    /*
                     * This should return feature ids that interects with the given geometry in the feature
                     * class having the index built.
                     */ 
                    queryLineByIdx.IntersectedFeatures(pnt.Shape, out idobjs);
                    lineIds = idobjs as int[];
                    if(null != lineIds && lineIds.Length > 0)
                    {
                        foreach(int id in lineIds)
                        {
                            line = fcLine.GetFeature(id);
                            lineFtrId = int.Parse(line.get_Value(fldDict["OBJECTID"]).ToString());
                            if ("1" == line.get_Value(fldDict["FlowDir"]).ToString() && true == requiredTypes.Contains(line.get_Value(fldDict["FTYPE"]).ToString()))
                            {
                                polyLine = line.Shape as IPolyline;
                                if(isSamePoint(polyLine.FromPoint, pnt.Shape as IPoint))
                                {
                                    queryPointByIdx.IntersectedFeatures(line.Shape, out idobjs1);
                                    pntIds = idobjs1 as int[];
                                    if(null != pntIds && 2 == pntIds.Length)
                                    {
                                        foreach(int pid in pntIds)
                                        {
                                            otherPnt = fcPoint.GetFeature(pid);
                                            if(false == isSamePoint(otherPnt.Shape as IPoint, pnt.Shape as IPoint))
                                            {
                                                tmpArr = new PolylineArrayClass();
                                                tmpArr.Add(polyLine);
                                                lengths = geoOperator.GetLengthsGeodesic(srLine, tmpArr as IPolylineArray, unit);
                                                if (0 == lengths.Count)
                                                    continue;
                                                lineLen = lengths.get_Element(0) * 3.28084;
                                                //var velos = from idx in range select double.Parse(line.get_Value(fldDict[fldLst[idx]]).ToString());
                                                List<double> velos = new List<double>();
                                                foreach(int idx in range)
                                                {
                                                    v = double.Parse(line.get_Value(fldDict[fldLst[idx]]).ToString());
                                                    velos.Add(v == 0 ? 0 : lineLen / v);
                                                }
                                                if (null == dsLst)
                                                    dsLst = new List<DownstreamNode>();
                                                dsLst.Add(new DownstreamNode(pid, id, velos));
                                             }
                                        }
                                    }
                                }
                                else // pnt at the end of the polyline
                                    ++incoming;
                            }
                        }
                    }
                    if(null != dsLst || incoming > 0)
                        nodeDict.Add(pntFtrId, new Vertex(int.Parse(pntFtrId), incoming, dsLst));
                    pnt = pntCur.NextFeature();
                    if (++count % 1000 == 0)
                        Console.WriteLine("Processing Count: " + count);
                    incoming = 0;
                    dsLst = null;
                }//end of while(null != pnt)
                ReleaseCOMObj(pntCur);
            }
            ReleaseCOMObj(workspaceFactory);
            //ESRI License Initializer generated code.
            //Do not make any call to ArcObjects after ShutDownApplication()
            m_AOLicenseInitializer.ShutdownApplication();
        }
    }
}
