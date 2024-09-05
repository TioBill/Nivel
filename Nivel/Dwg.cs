using System;
using System.Diagnostics;
using System.Reflection;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.Runtime;

namespace Nivel
{
    public sealed class Dwg
    {  
        public Document AcDocument { get; set; }
        public Editor AcEditor { get; set; }
        public Database AcDatabase { get; set; }
        public Matrix3d UCS { get; set; }

        private static Dwg instance;

        private Dwg()
        {
            AcDocument = null;
            AcEditor = null;
            AcDatabase = null;
            UCS = new Matrix3d();
        }

        public static Dwg GetInstance()
        {
            if (instance == null)
                instance = new Dwg();

            return instance;
        }

        public void UpdateDwg()
        {
            AcDocument = Application.DocumentManager.MdiActiveDocument;
            AcEditor = AcDocument.Editor;
            AcDatabase = AcDocument.Database;
            UCS = AcEditor.CurrentUserCoordinateSystem;
        }

        public void addEventPointMonitor()
        {
            if (!IsHandlerSubscribed(AcEditor_PointMonitor))
                AcEditor.PointMonitor += AcEditor_PointMonitor;
        }

        public void removeEventPointMonitor()
        {
            AcEditor.PointMonitor -= AcEditor_PointMonitor;
        }

        private bool IsHandlerSubscribed(Action<object, PointMonitorEventArgs> handler)
        {
            var eventField = typeof(Editor).GetField("PointMonitor", BindingFlags.NonPublic | BindingFlags.Instance);

            if (eventField?.GetValue(AcEditor) is Delegate eventDelegate)
            {
                foreach (Delegate existingHandler in eventDelegate.GetInvocationList())
                {
                    if (existingHandler.Equals(handler))
                        return true;
                }
            }

            return false;
        }

        private void AcEditor_PointMonitor(object sender, PointMonitorEventArgs e)
        {
            var editor = (Editor)sender;

            if (editor == null) return;

            Point3d MousePosition = e.Context.ComputedPoint;

            if (Main.CanCapture)
            {
                Main.MainPanel.CurrentCoords_TB.Text = "Current Level: " + Main.CalculateLevel(Main.RefRes.Value, MousePosition, Main.LevelRes.Value, true);
                Main.MainPanel.CurrentScale_TB.Text = "Current Scale: " + Main.ScaleValue.ToString();
            }
        }
    }
}
