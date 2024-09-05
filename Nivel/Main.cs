using System;
using System.Windows.Input;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.EditorInput;
using ZwSoft.ZwCAD.Geometry;
using ZwSoft.ZwCAD.Runtime;


/*
 * TODO: Fazer com que na hora de adicionar a cota, ele tambem adicione qual o tipo de cota.
 * 
 * Deixar como os padrões
 * 
 * N.Terreno (Nivel de Terreno), N.C (Nivel de calçada), N.A.MIN (Nivel de água minimo), N.A.MAX (Nivel de água máximo) e N.Topo (Nivel de topo)
 * Porem também deixe a oportunidade do usuário poder escolher qual ele quer, podem ser qualquer coisa.
 * 
 * Uma combobox poderia ser bom. Quando o usuario adicionar, ele fica dentro da combobox, facilitando o usuario escolher a marcação.
 * 
 */

namespace Nivel
{

    public class Main
    {
        private static Dwg currentDwg = Dwg.GetInstance();
        public static StateCode State { get; set; }
        public static ControlPanel MainPanel { get; set; }
        public static PromptPointResult RefRes { get; set; }
        public static PromptDoubleResult LevelRes { get; set; }
        public static double ScaleValue { get; set; }
        public static bool CanCapture { get; set; }

        private static void StartProgram()
        {
            if (MainPanel == null)
                MainPanel = new ControlPanel();

            currentDwg.UpdateDwg();

            State = StateCode.DrawText;
            currentDwg.addEventPointMonitor();
            CanCapture = false;

            MainPanel.Show();
        }

        private static void EndProgram()
        {
            MainPanel.Close();
            currentDwg.removeEventPointMonitor();

            MainPanel = null;
            RefRes = null;
            LevelRes = null;
            CanCapture = false;
        }

        [CommandMethod("Nivel")]
        public static void Nivel()
        {
            StartProgram();

            RefRes = GetPoint("Clique no ponto base:");

            if (RefRes.Status != PromptStatus.OK)
            {
                EndProgram();
                return;
            }

            LevelRes = GetDouble("Digite o nivel referencia:");

            if (LevelRes.Status != PromptStatus.OK)
            {
                EndProgram();
                return;
            }

            PromptDoubleResult TamanhoTexto = GetDouble("Digitie o tamanho de texto:");

            if (TamanhoTexto.Status != PromptStatus.OK)
            {
                EndProgram();
                return;
            }

            PromptDoubleResult Scale = GetDouble("Digite a escala [100]:", true);
            
            if (Scale.Status == PromptStatus.Error)
            {
                EndProgram();
                return;
            }

            if (Scale.Status == PromptStatus.None)
                ScaleValue = 100;
            else
                ScaleValue = Scale.Value;

            CanCapture = true;

            while (true)
            {
                PromptPointResult NewPointRes = GetPoint("Novo ponto:");

                if (NewPointRes.Status != PromptStatus.OK) break;

                switch(State)
                {
                    case StateCode.ChangeText:
                        ChangeText(
                            GetText("Selecione um MText ou Texto:"),
                            CalculateLevel(RefRes.Value, NewPointRes.Value, LevelRes.Value));
                        break;
                    case StateCode.DrawText:
                        PutText(
                            NewPointRes.Value,
                            CalculateLevel(RefRes.Value, NewPointRes.Value, LevelRes.Value), TamanhoTexto.Value);
                        break;
                    case StateCode.DrawPinPoint:
                        DrawBlock(NewPointRes.Value, TamanhoTexto.Value);
                        PutText( 
                            new Point3d(NewPointRes.Value.X, NewPointRes.Value.Y + TamanhoTexto.Value * 2.5, 0),
                            CalculateLevel(RefRes.Value, NewPointRes.Value, LevelRes.Value), TamanhoTexto.Value);
                        break;
                }
            }

            EndProgram();
        }

        private static void ChangeText(PromptEntityResult newText, string level)
        {

            using (Transaction trans = currentDwg.AcDocument.TransactionManager.StartTransaction())
            {
                Entity text = trans.GetObject(newText.ObjectId, OpenMode.ForWrite) as Entity;

                if (text.GetType() == typeof(MText))
                    ((MText)text).Contents = level;
                else
                    ((DBText)text).TextString = level;

                trans.Commit();
            }
        }

        private static PromptEntityResult GetText(string message)
        {
            PromptEntityOptions entOpts = new PromptEntityOptions(message);
            entOpts.SetRejectMessage("Apenas MText ou Text sao permitidos...");

            entOpts.AddAllowedClass(typeof(DBText), true);
            entOpts.AddAllowedClass(typeof(MText), true);

            entOpts.AllowNone = false;

            return currentDwg.AcEditor.GetEntity(entOpts);
        }

        private static void DrawBlock(Point3d blockPosition, double tamanho)
        {
            blockPosition = blockPosition.TransformBy(currentDwg.UCS);

            using (Transaction trans = currentDwg.AcDocument.TransactionManager.StartTransaction())
            {
                BlockTable acBlockTable = trans.GetObject(currentDwg.AcDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTableRecord = trans.GetObject(acBlockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Right side
                using (Polyline block = new Polyline())
                {
                    block.AddVertexAt(0, new Point2d(blockPosition.X, blockPosition.Y), 0, 0, 0);
                    block.AddVertexAt(1, new Point2d(blockPosition.X, blockPosition.Y + tamanho * 1.86), 0, 0, 0);
                    block.AddVertexAt(2, new Point2d(blockPosition.X + tamanho * 1.06, blockPosition.Y + tamanho * 1.86), 0, 0, 0);

                    block.Closed = true;

                    acBlkTableRecord.AppendEntity(block);
                    trans.AddNewlyCreatedDBObject(block, true);
                }

                // Left side
                using (Polyline block = new Polyline())
                {
                    block.AddVertexAt(0, new Point2d(blockPosition.X, blockPosition.Y), 0, 0, 0);
                    block.AddVertexAt(1, new Point2d(blockPosition.X, blockPosition.Y + tamanho * 1.86), 0, 0, 0);
                    block.AddVertexAt(2, new Point2d(blockPosition.X - tamanho * 1.06, blockPosition.Y + tamanho * 1.86), 0, 0, 0);

                    block.Closed = true; 

                    acBlkTableRecord.AppendEntity(block);
                    trans.AddNewlyCreatedDBObject(block, true);

                    // Draw hatch
                    ObjectIdCollection acObjIdColl = new ObjectIdCollection();
                    acObjIdColl.Add(block.ObjectId);

                    using (Hatch hatch = new Hatch())
                    {
                        hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                        hatch.Associative = true;
                        hatch.AppendLoop(HatchLoopTypes.Outermost, acObjIdColl);
                        hatch.EvaluateHatch(true);

                        acBlkTableRecord.AppendEntity(hatch);
                        trans.AddNewlyCreatedDBObject(hatch, true);
                    }
                }

                // SLASH
                using (Polyline block = new Polyline())
                {
                    block.AddVertexAt(0, new Point2d(blockPosition.X - tamanho * 1.06, blockPosition.Y + tamanho * 2), 0, 0, 0);
                    block.AddVertexAt(1, new Point2d(blockPosition.X + tamanho * 4, blockPosition.Y + tamanho * 2), 0, 0, 0);

                    acBlkTableRecord.AppendEntity(block);
                    trans.AddNewlyCreatedDBObject(block, true);
                }


                trans.Commit();
            }
        }

        private static void PutText(Point3d textPosition, string level, double textHeight)
        {
            textPosition = textPosition.TransformBy(currentDwg.UCS);

            using (Transaction trans = currentDwg.AcDocument.TransactionManager.StartTransaction())
            {
                BlockTable acBlockTable = trans.GetObject(currentDwg.AcDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord acBlkTableRecord = trans.GetObject(acBlockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                using (DBText text = new DBText())
                {
                    text.Position = textPosition;
                    text.TextString = level;
                    text.Height = textHeight;

                    acBlkTableRecord.AppendEntity(text);
                    trans.AddNewlyCreatedDBObject(text, true);
                }

                trans.Commit();
            }
        }

        public static string CalculateLevel(Point3d initialPosition, Point3d finalPosition, Double valRef, bool mousePos=false)
        {
            initialPosition = initialPosition.TransformBy(currentDwg.UCS);

            if (!mousePos)
                finalPosition = finalPosition.TransformBy(currentDwg.UCS);

            return Math.Round((finalPosition.Y - initialPosition.Y) / ScaleValue + valRef, 3).ToString();
        }

        private static PromptPointResult GetPoint(string message)
        {
            PromptPointOptions promptPointOptions = new PromptPointOptions(message);
            promptPointOptions.AllowNone = false;

            return currentDwg.AcEditor.GetPoint(promptPointOptions);
        }

        private static PromptDoubleResult GetDouble(string message, bool allowNone=false)
        {
            PromptDoubleOptions doubleOpts = new PromptDoubleOptions(message);

            doubleOpts.AllowNone = allowNone;
            doubleOpts.AllowNegative = false;
            doubleOpts.AllowZero = false;

            return currentDwg.AcEditor.GetDouble(doubleOpts);
        }
    }
}
