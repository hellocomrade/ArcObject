using System;
using System.IO;
using System.Data.OleDb;
using System.Collections.Generic;
using System.Text;
using ESRI.ArcGIS;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ESRIDesktopConsoleApplication1
{
    class Program
    {
        private static LicenseInitializer m_AOLicenseInitializer = new ESRIDesktopConsoleApplication1.LicenseInitializer();
        private const string ConnStrFMT = @"Provider=Microsoft.Jet.OleDb.4.0; Data Source={0};Extended Properties=""Text;HDR=YES;FMT=Delimited""";
        private const string GDB_Name = "glrd-{0}.gdb";
        private const string OBJECT_ID = "OID";
        private const string SHP_NAME = "Shape";
        private const string FtrClass_Name = "glrd";
        
        static List<dynamic> ParseCSV(string filename, string statename)
        {
            List<dynamic> prjs = new List<dynamic>();
            if(true == File.Exists(filename))
            {
                var connStr = string.Format(ConnStrFMT, System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(filename)));
                try
                {
                    using (var conn = new OleDbConnection(connStr))
                    {
                        conn.Open();
                        using (var cmd = new OleDbCommand(string.Format("select [lon],[lat],[Title] from [{0}] where UCase([State]) like '%{1}%'", System.IO.Path.GetFileName(filename), statename.ToUpper()),conn))
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                    prjs.Add(new { X = reader.GetDouble(0), Y = reader.GetDouble(1), Name = reader.GetString(2) });
                            }
                        }

                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            return prjs;
        }
        static void ReleaseCOMObj(object comobj)
        {
            int refCount = 0;
            do{
                refCount = System.Runtime.InteropServices.Marshal.ReleaseComObject(comobj);
            }while(refCount > 0);
        }
        static ISpatialReference CreateSpatialRef(int epsg)
        {
            ISpatialReferenceFactory factory = new SpatialReferenceEnvironmentClass();
            return factory.CreateGeographicCoordinateSystem(epsg);
        }
        static IGeometryDefEdit CreateGeometryDef(esriGeometryType type, int epsg)
        {
            IGeometryDefEdit geomDef = new GeometryDefClass();
            geomDef.GeometryType_2 = type;
            geomDef.SpatialReference_2 = CreateSpatialRef(epsg);
            return geomDef;
        }
        //http://resources.esri.com/help/9.3/arcgisengine/arcobjects/esriGeoDatabase/IFeatureWorkspace.CreateFeatureClass_Example.htm
        static IFeatureClass CreateFeatureClass(string name, IFeatureWorkspace ftrSpc, esriGeometryType type, int epsg, List<dynamic> extraFields)
        {
            IFeatureClass ftrc = null;
            if(null != ftrSpc && null != name)
            {
                IFieldsEdit flds = new FieldsClass();
                flds.FieldCount_2 = 2 + (extraFields == null ? 0 : extraFields.Count);

                IFieldEdit fld = new FieldClass();
                fld.Name_2 = OBJECT_ID;
                fld.Type_2 = esriFieldType.esriFieldTypeOID;
                flds.Field_2[0] = fld;

                fld = new FieldClass();
                fld.Name_2 = SHP_NAME;
                fld.Type_2 = esriFieldType.esriFieldTypeGeometry;
                fld.GeometryDef_2 = CreateGeometryDef(type, epsg);
                flds.Field_2[1] = fld;
                int eidx = 2;
                foreach(var efld in extraFields)
                {
                    fld = new FieldClass();
                    fld.Name_2 = efld.Name;
                    fld.Type_2 = efld.Type;
                    flds.Field_2[eidx++] = fld;
                }
                ftrc = ftrSpc.CreateFeatureClass(name, flds, null, null, esriFeatureType.esriFTSimple, SHP_NAME, null);
            }
            return ftrc;
        }
        //not used
        static bool ifGDBExist(IWorkspace2 ws2, string gdb)
        {
            return ws2.get_NameExists(esriDatasetType.esriDTFeatureClass, gdb);
        }
        [STAThread()]
        static void Main(string[] args)
        {
            if(2 != args.Length)
            {
                System.Console.WriteLine("We need that GLRD csv file in this demo, dude! Also, tell me the State Name as filter please!");
                return;
            }
            string gdb = string.Format(GDB_Name, args[1]);
            var prjs = ParseCSV(args[0], args[1]);
            if (prjs.Count > 0)
            {
                try
                {
                    //ESRI License Initializer generated code.
                    m_AOLicenseInitializer.InitializeApplication(new esriLicenseProductCode[] { esriLicenseProductCode.esriLicenseProductCodeAdvanced },
                    new esriLicenseExtensionCode[] { esriLicenseExtensionCode.esriLicenseExtensionCodeNetwork, esriLicenseExtensionCode.esriLicenseExtensionCodeSpatialAnalyst });
                    //print version info to console
                    var runtimes = RuntimeManager.InstalledRuntimes;
                    foreach (RuntimeInfo runtime in runtimes)
                    {
                        System.Console.WriteLine(runtime.Path);
                        System.Console.WriteLine(runtime.Version);
                        System.Console.WriteLine(runtime.Product.ToString());
                    }
                    // Instantiate a file geodatabase workspace factory and create a file geodatabase.
                    // The Create method returns a workspace name object.
                    // Ugly way to create object through reflection
                    Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
                    IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
                    if (false == System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(args[0])) + "\\" + gdb))
                    {
                        IWorkspaceName workspaceName = workspaceFactory.Create(System.IO.Path.GetDirectoryName(args[0]), gdb, null, 0);
                        //ugly way to release raw COM Object
                        ReleaseCOMObj(workspaceFactory);
                        // Cast the workspace name object to the IName interface and open the workspace.
                        IName name = (IName)workspaceName;
                        IWorkspace workspace = (IWorkspace)name.Open();
                        IFeatureWorkspace ftrspace = workspace as IFeatureWorkspace;
                        List<dynamic> fldlst = new List<dynamic>();
                        fldlst.Add(new { Name = "Title", Type = esriFieldType.esriFieldTypeString });
                        IFeatureClass ftrClass = CreateFeatureClass(FtrClass_Name, ftrspace, esriGeometryType.esriGeometryPoint, 4326, fldlst);
                        if (null != ftrClass)
                        {
                            IPoint pnt = null;
                            double x, y;
                            string title = null;
                            int idxTitle = ftrClass.Fields.FindField("Title");
                            //http://edndoc.esri.com/arcobjects/9.2/NET/e7b33417-0373-4f87-93db-074910907898.htm
                            // Create the feature buffer.
                            IFeatureBuffer ftrBuffer = ftrClass.CreateFeatureBuffer();
                            // Create insert feature cursor using buffering.
                            IFeatureCursor ftrCursor = ftrClass.Insert(true);
                            foreach (var prj in prjs)
                            {
                                x = prj.X;
                                y = prj.Y;
                                title = prj.Name;
                                pnt = new PointClass();
                                pnt.PutCoords(x, y);
                                ftrBuffer.Shape = pnt;
                                ftrBuffer.set_Value(idxTitle, title);
                                ftrCursor.InsertFeature(ftrBuffer);
                            }
                            try
                            {
                                ftrCursor.Flush();
                                Console.WriteLine(string.Format("File GDB: {0} has been created!", gdb));
                            }
                            catch (System.Runtime.InteropServices.COMException)
                            {
                                throw;
                            }
                            finally
                            {
                                ReleaseCOMObj(ftrCursor);
                            }
                        }
                    }
                    else
                        Console.WriteLine(string.Format("Dude, the GDB has been created already! Remove {0} and try again please.", gdb));
                    //ESRI License Initializer generated code.
                    //Do not make any call to ArcObjects after ShutDownApplication()
                    m_AOLicenseInitializer.ShutdownApplication();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}
