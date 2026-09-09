using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;

[assembly: CommandClass(typeof(CAD2DTo3D.Commands))]

namespace CAD2DTo3D
{
    public class Commands
    {
        [CommandMethod("2DTo3D")]
        public static void Convert2DTo3D()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 步骤1：提示用户选择2D图形
                ed.WriteMessage("\n========== 2D转3D挤出工具 ==========\n");
                ed.WriteMessage("请框选要挤出的2D图形（可多选）...\n");

                PromptSelectionOptions pso = new PromptSelectionOptions();
                pso.MessageForAdding = "选择2D图形: ";
                pso.MessageForRemoval = "移除图形: ";
                pso.AllowDuplicates = false;

                PromptSelectionResult psr = ed.GetSelection(pso);

                if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
                {
                    ed.WriteMessage("\n未选择任何图形。操作已取消。\n");
                    return;
                }

                // 步骤2：显示方向选择对话框
                ExtradeDialog dialog = new ExtradeDialog();
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    ed.WriteMessage("\n操作已取消。\n");
                    return;
                }

                // 获取用户输入的参数
                string direction = dialog.SelectedDirection;
                double height = dialog.ExtrudeHeight;

                ed.WriteMessage($"\n选择方向: {direction}, 挤出高度: {height}\n");

                // 步骤3：执行挤出操作
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(
                        db.CurrentSpaceId, OpenMode.ForWrite);

                    SelectionSet ss = psr.Value;
                    int successCount = 0;

                    foreach (SelectedObject so in ss)
                    {
                        ObjectId id = so.ObjectId;
                        Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);

                        // 检查是否是可以挤出的2D实体
                        if (IsExtrudable(ent))
                        {
                            try
                            {
                                // 执行挤出
                                Entity extrudedEntity = Extrude3DLogic.ExtrudeEntity(
                                    ent, direction, height, db, tr);

                                if (extrudedEntity != null)
                                {
                                    btr.AppendEntity(extrudedEntity);
                                    tr.AddNewlyCreatedDBObject(extrudedEntity, true);
                                    successCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                ed.WriteMessage($"\n挤出失败: {ex.Message}");
                            }
                        }
                        else
                        {
                            ed.WriteMessage($"\n警告: 选中的实体不可挤出\n");
                        }
                    }

                    tr.Commit();
                    ed.WriteMessage($"\n成功挤出 {successCount} 个图形！\n");
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n错误: {ex.Message}\n");
            }
        }

        /// <summary>
        /// 检查实体是否可以挤出
        /// </summary>
        private static bool IsExtrudable(Entity ent)
        {
            return ent is Polyline ||
                   ent is Polyline2d ||
                   ent is Polyline3d ||
                   ent is Circle ||
                   ent is Ellipse ||
                   ent is Arc ||
                   ent is Spline ||
                   ent is Region;
        }
    }
}
