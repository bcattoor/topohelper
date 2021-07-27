/*
 * Created by Cattoor Bjorn
 * Datum: 17/10/2017
 * Tijd: 11:41
 *
 * For licencing information see incuded licence file.
 */

using System;
using System.Diagnostics;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using TopoHelper.Autocad;
using TopoHelper.AutoCAD;

namespace TopoHelper.CommandImplementations
{
    /// <summary>
    /// Description of PlaceTextOnLineWithLength.
    /// </summary>
    public static class PlaceTextOnLineWithLength
    {
        public static void ExcecuteCommand(bool fliptext, string functionCanceledMessage)
        {
            const string textStyleName = "IAP-LINE-DIST";
            const string fontName = "Gautami";
            const double textHeight = 1.0;
            const double textWidth = 0.8;
            const string layerName = "501-12";
            const int layerColor = 7;
            const double offset = 0.8;
            const string layerDescription = "Laag met lijnstuk afstanden.";

            var currentDocument = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            var editor = currentDocument.Editor;

            // Create the style we will use
            PlaceTextOnLineWithLength.AddTextStyle(textStyleName, fontName,
                textHeight, textWidth);

            var textStyleId = GetTextStyleId(textStyleName);
        label_here:
            // get two point from database, and create a line element. we can
            // the pass that line element to our Jig-function
            using (currentDocument.LockDocument())
            {
                var pointA = editor.Get3dPoint();
                if (!pointA.HasValue) { throw new Exception(functionCanceledMessage); }
                var pointB = editor.Get3dPoint();
                if (!pointB.HasValue) { throw new Exception(functionCanceledMessage); }

                // create the line
                using (var line = new Line(pointA.Value, pointB.Value))
                using (var newRotatedText = new MText())
                {
                    newRotatedText.SetDatabaseDefaults();
                    newRotatedText.TextStyleId = textStyleId;
                    newRotatedText.TextHeight = textHeight;
                    newRotatedText.Contents = line.Length.ToString("L ######.000");
                    CreateLayer(layerName, layerColor, layerDescription);
                    newRotatedText.Layer = layerName;

                    var myEntityJig = new EntityTextAlignJig(newRotatedText, line, offset, fliptext, textHeight);

                //+Drag JIG

                label2:
                    var res = editor.Drag(myEntityJig);
                    if (res.Status == PromptStatus.Cancel || res.Status == PromptStatus.Error)
                        return;
                    if (res.Status == PromptStatus.Keyword)
                        goto label2;

                    //+ Probably canceled, just return, do nothing.
                    if (myEntityJig == null)
                        return;

                    //Lets place our text to the drawing

                    using (var tr = currentDocument.TransactionManager.StartOpenCloseTransaction())
                    {
                        //' Open the Block table for read
                        var myBlkTbl = (BlockTable)tr.GetObject(currentDocument.Database.BlockTableId, OpenMode.ForRead);

                        //' Open the Block table record for read

                        //' Get the ObjectID for Model space from the Block table
                        var myBlkTblRec = (BlockTableRecord)tr.GetObject(myBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        myBlkTblRec.AppendEntity(newRotatedText);

                        tr.AddNewlyCreatedDBObject(newRotatedText, true);

                        //' Save the new text to the database
                        tr.Commit();
                    }
                }

                goto label_here;
            }
        }

        public static bool CreateLayer(string layName, short colorIndex, string description, bool layerIsLocked = false, bool isPlottable = true, bool isHidden = false, LineWeight lineWeight = LineWeight.ByLineWeightDefault)
        {
            var document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;

            var database = document.Database;

            var transAction = database.TransactionManager.StartOpenCloseTransaction();

            using (transAction)
            {
                // Get the layer table from the drawing
                var layerTable = (LayerTable)transAction.GetObject(database.LayerTableId, OpenMode.ForRead);

                //make sure name is valid according to the SymbolUtilityService
                SymbolUtilityServices.ValidateSymbolName(layName, false);

                if (layerTable.Has(layName))
                {
                    //we return true, we have the layer already... (so you could say it has been created)
                    return true;
                }

                //create the layer
                var newLayerTableRecord = new LayerTableRecord();
                try
                {
                    // Add the new layer to the layer table
                    layerTable.UpgradeOpen();

                    layerTable.Add(newLayerTableRecord);

                    transAction.AddNewlyCreatedDBObject(newLayerTableRecord, true);
                    // ... and set its properties
                    newLayerTableRecord.Name = layName;
                    newLayerTableRecord.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, colorIndex);
                    newLayerTableRecord.Description = description;
                    newLayerTableRecord.IsLocked = layerIsLocked;
                    newLayerTableRecord.IsHidden = isHidden;
                    newLayerTableRecord.IsPlottable = isPlottable;
                    newLayerTableRecord.LineWeight = lineWeight;
                    transAction.Commit();
                }
                finally
                {
                    newLayerTableRecord.Dispose();
                    layerTable.Dispose();
                }

                return true;
            }
        }

        public static ObjectId AddTextStyle(string name, string fontname, double textHeight, double widthFactor, bool isShapeFile = false, bool isVertical = false, bool annotative = false, double obliqueAngle = 0.0, bool orientationToLayout = false, bool upsideDown = false, bool backwards = false)
        {
            var retId = ObjectId.Null;
            var db = HostApplicationServices.WorkingDatabase;
            var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;

            using (doc.LockDocument())
            {
                using (var trans = db.TransactionManager.StartOpenCloseTransaction())
                {
                    var currentTextStyleTable = (TextStyleTable)trans.GetObject(db.TextStyleTableId, OpenMode.ForWrite, false);
                    if (!currentTextStyleTable.Has(name))
                    {
                        var textStyle = new TextStyleTableRecord
                        {
                            Name = name,
                            Font =
                                new FontDescriptor(fontname, false, false, 0, 0),
                            Annotative = annotative ? AnnotativeStates.True : AnnotativeStates.False,
                            ObliquingAngle = obliqueAngle
                        };
                        textStyle.SetPaperOrientation(orientationToLayout);
                        textStyle.FlagBits = 0;
                        textStyle.FlagBits += upsideDown ? (byte)2 : (byte)0;
                        textStyle.FlagBits += backwards ? (byte)4 : (byte)0;
                        textStyle.TextSize = textHeight;
                        textStyle.XScale = widthFactor;
                        textStyle.IsVertical = isVertical;
                        textStyle.IsShapeFile = isShapeFile;
                        retId = currentTextStyleTable.Add(textStyle);
                        trans.AddNewlyCreatedDBObject(textStyle, true);
                    }
                    trans.Commit();
                }
            }

            return retId;
        }

        public static double GetTextStyleHeight(string name)
        {
            var acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            var acCurDb = acDoc.Database;

            using (var transAction = acCurDb.TransactionManager.StartOpenCloseTransaction())
            {
                var textStyleTable = transAction.GetObject(acDoc.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

                if (textStyleTable == null || !textStyleTable.Has(name))
                    return 0.0;
                var tx = (TextStyleTableRecord)GetObject(textStyleTable[name]);
                using (tx)
                {
                    return tx.TextSize;
                }
            }
        }

        /// <summary>
        /// Get an object form the database using its object ID Get an object
        /// form the database using its object ID
        /// </summary>
        /// <param name="objectIDx"> Object ID </param>
        /// <returns> Object </returns>
        /// <remarks> After retrieving object cast it to its type! </remarks>
        public static object GetObject(ObjectId objectIDx)
        {
            var acCurDb = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;
            var myDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;

            using (var acTrans = acCurDb.TransactionManager.StartOpenCloseTransaction())
            {
                return acTrans.GetObject(objectIDx, OpenMode.ForRead);
            }
        }

        public static ObjectId GetTextStyleId(string name)
        {
            var acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            var acCurDb = acDoc.Database;

            using (var transAction = acCurDb.TransactionManager.StartOpenCloseTransaction())
            {
                var textStyleTable = transAction.GetObject(acDoc.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

                if (textStyleTable == null || !textStyleTable.Has(name))
                    throw new Exception("The active text style was not found in the current database.");

                var tx = (TextStyleTableRecord)GetObject(textStyleTable[name]);
                using (tx)
                {
                    return tx.ObjectId;
                }
            }
        }
    }

    public class EntityTextAlignJig : EntityJig
    {
        private Arc _baseArc;

        private Point3d _baseArcCenterPoint;
        private double _baseArcRadius;
        private Line _BaseLine;
        private double _beginFrontierAngle;

        private double _endFrontierAngle;

        private bool _fliptexst;

        private double _offset;

        private double _selectedAngle;

        private double _selectedLength;

        private Point3d _selectedpoint;

        private double _textHeight;

        public EntityTextAlignJig(Entity ent, Arc baseArc, double offset, bool flip, double heightText)
            : base(ent)
        {
            _offset = offset;
            _baseArcCenterPoint = baseArc.Center;
            _baseArcRadius = baseArc.Radius;
            _baseArc = baseArc;
            _fliptexst = flip;
            _textHeight = heightText;
        }

        public EntityTextAlignJig(Entity ent, Line baseLine, double offset, bool flip, double heightText)
            : base(ent)
        {
            _offset = offset;
            _fliptexst = flip;
            _textHeight = heightText;
            _BaseLine = baseLine;
        }

        public int CurrentInputValue
        {
            get;
            set;
        }

        public Point3d TextPositionPoint
        {
            get;
            set;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            try
            {
                //We need to check witch input we are on

                Point3d oldpoint = _selectedpoint;
                JigPromptPointOptions opt = new JigPromptPointOptions("\nSelect where on entity to place text");
                opt.Keywords.Add("Flip");

                PromptPointResult jigPromtResult = prompts.AcquirePoint(opt);

                //Now check the status
                if (jigPromtResult.Status == PromptStatus.Keyword)
                {
                    if (jigPromtResult.StringResult == "Flip")
                    {
                        if (_fliptexst == true)
                        {
                            _fliptexst = false;
                        }
                        else
                        {
                            _fliptexst = true;
                        }
                    }
                }
                else if (jigPromtResult.Status == PromptStatus.OK)
                {
                    //extract out the point from the result
                    _selectedpoint = jigPromtResult.Value;

                    //Check to see if the user made a change
                    if (oldpoint.DistanceTo(_selectedpoint) < 0.001)
                    {
                        return SamplerStatus.NoChange;
                    }
                }

                //Otherwise return the status
                return SamplerStatus.OK;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return SamplerStatus.NoChange;
            }
            finally
            {
            }
        }

        protected override bool Update()
        {
            //We need to check witch input we are on
            if (_baseArc != null)
            {
                return updateArc();
            }
            if (_BaseLine != null)
            {
                return updateLine();
            }
            return false;
        }

        private bool updateArc()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            bool isOnInsideOfArc = false;

            Arc myOffsetArc = null;
            Line userGeneretedLine = null;
            Point3dCollection intersectionPointColection = null;
            try
            {
                //this routine gets constantly called to calculate the updated Jig

                #region Check if the baseArcCenterpoint = selectedpoint If true, the no update is needed

                if (_selectedpoint == _baseArcCenterPoint)
                {
                    _selectedpoint = new Point3d(_selectedpoint.X, _selectedpoint.Y + 0.5, 0);
                }

                #endregion Check if the baseArcCenterpoint = selectedpoint If true, the no update is needed

                #region Creat a imagenary [userGeneretedLine] line between the centerpoint and the selectedpoint

                userGeneretedLine = new Line(_baseArcCenterPoint, _selectedpoint);
                userGeneretedLine.SetDatabaseDefaults();

                #endregion Creat a imagenary [userGeneretedLine] line between the centerpoint and the selectedpoint

                #region Get angle and length from this line

                _selectedLength = userGeneretedLine.Length;
                _selectedAngle = userGeneretedLine.Angle;

                #endregion Get angle and length from this line

                #region Nu we de hoek hebben van de gegenereerde lijn moeten we ons afvragen ofdat de hoek de grenswaarden niet overschrijft,dus halen we eerst de grenswaarden van de hoeken er uit

                _beginFrontierAngle = _baseArc.StartAngle;
                _endFrontierAngle = _baseArc.EndAngle;

                #endregion Nu we de hoek hebben van de gegenereerde lijn moeten we ons afvragen ofdat de hoek de grenswaarden niet overschrijft,dus halen we eerst de grenswaarden van de hoeken er uit

                #region de hoek van de geselecteerde lijn is niet "valid" als deze buiten het hoekbereik van onze boog ligt: if _selectedAngele > starthoek then ok; and if _selectedAngle < einhoek then ook ok, else niet ok

                if (_beginFrontierAngle < _endFrontierAngle)
                {
                    if (_selectedAngle < _beginFrontierAngle || _selectedAngle > _endFrontierAngle)
                    {// word deze voorwaarde voldaan gaan we gewoon door
                        //De hoek gaat buiten de zone, hier berekenen we welke hoek we moeten gebruiken in de plaats,
                        //de eindhoek of de starthoek
                        int diffirenceEndAngleSelectedAngle = Math.Abs((int)((_endFrontierAngle - _selectedAngle) * 1000));
                        int diffirenceBeginAngleSelectedAngle = Math.Abs((int)((_beginFrontierAngle - _selectedAngle) * 1000));

                        if (diffirenceBeginAngleSelectedAngle >= diffirenceEndAngleSelectedAngle)
                        {
                            _selectedAngle = _endFrontierAngle;
                            userGeneretedLine.EndPoint = _baseArc.EndPoint;
                        }
                        else
                        {
                            _selectedAngle = _beginFrontierAngle;
                            userGeneretedLine.EndPoint = _baseArc.StartPoint;
                        }
                    }
                }
                else
                { //Boog begint met eindhoek, en eindigt met beginhoek (aanpassing 19 oktober 2010|| Cattoor Bjorn)
                    if (_selectedAngle > _beginFrontierAngle || _selectedAngle < _endFrontierAngle)
                    {// word deze voorwaarde voldaan gaan we gewoon door
                        //doe niets
                    }
                    else
                    {
                        //De hoek gaat buiten de zone, hier berekenen we welke hoek we moeten gebruiken in de plaats,
                        //de eindhoek of de starthoek
                        int diffirenceEndAngleSelectedAngle = Math.Abs((int)((_endFrontierAngle - _selectedAngle) * 1000));
                        int diffirenceBeginAngleSelectedAngle = Math.Abs((int)((_beginFrontierAngle - _selectedAngle) * 1000));

                        if (diffirenceBeginAngleSelectedAngle >= diffirenceEndAngleSelectedAngle)
                        {
                            _selectedAngle = _endFrontierAngle;
                            userGeneretedLine.EndPoint = _baseArc.EndPoint;
                        }
                        else
                        {
                            _selectedAngle = _beginFrontierAngle;
                            userGeneretedLine.EndPoint = _baseArc.StartPoint;
                        }
                    }
                }

                #endregion de hoek van de geselecteerde lijn is niet "valid" als deze buiten het hoekbereik van onze boog ligt: if _selectedAngele > starthoek then ok; and if _selectedAngle < einhoek then ook ok, else niet ok

                #region Nu moeten we uitzoeken ofdat de user wil dat de tekst aan de binnenzijde

                //of aan de buitenzijde van de boog staat

                if (_selectedLength >= _baseArc.Radius)
                {
                    isOnInsideOfArc = true;
                }

                #endregion Nu moeten we uitzoeken ofdat de user wil dat de tekst aan de binnenzijde

                //We starten met het berekenen van een nieuwe arc, met als radius (radius_baseArc) - of + (Naargelang binnen
                //kant of buitenkant van de offset[isOnInsideOfArc]) de offset distance.

                #region berekenen offsetArc

                /*                myOffsetLine1 = (Line)_BaseLine.GetOffsetCurves(_offset + _textHeight/2)[0];
                myOffsetLine2 = (Line)_BaseLine.GetOffsetCurves(-(_offset + _textHeight/2))[0];*/

                if (_fliptexst)
                {
                    if (isOnInsideOfArc)
                    {
                        // myOffsetArc = (Arc)_baseArc.GetOffsetCurves(_offset + (((DBText)this.Entity).Height))[0];

                        myOffsetArc = (Arc)_baseArc.GetOffsetCurves(_offset + _textHeight / 2)[0];
                    }
                    else
                    {
                        // myOffsetArc = (Arc)_baseArc.GetOffsetCurves(-_offset
                        // - (35 / _baseArc.Radius))[0];

                        myOffsetArc = (Arc)_baseArc.GetOffsetCurves(-(_offset + _textHeight / 2))[0];
                    }
                }
                else
                {
                    if (isOnInsideOfArc)
                    {
                        // myOffsetArc = (Arc)_baseArc.GetOffsetCurves(+_offset)[0];

                        myOffsetArc = (Arc)_baseArc.GetOffsetCurves(_offset + _textHeight / 2)[0];
                    }
                    else
                    {
                        // myOffsetArc = (Arc)_baseArc.GetOffsetCurves(-(_offset
                        // + ((DBText)this.Entity).Height + (35 / _baseArc.Radius)))[0];

                        myOffsetArc = (Arc)_baseArc.GetOffsetCurves(-(_offset + _textHeight / 2))[0];
                    }
                }

                #endregion berekenen offsetArc

                Point3d textPositionPoint = _baseArc.StartPoint;

                #region //Zoeken naar snijdingspunt userGeneretedLine - offsetarc

                var myPlaneWCS = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));
                intersectionPointColection = new Point3dCollection();

                //userGeneretedLine.IntersectWith(myOffsetArc, Intersect.ExtendThis, intersectionPointColection, 0, 0); OBSOLETE CODE
                userGeneretedLine.IntersectWith(myOffsetArc, Intersect.ExtendThis, myPlaneWCS, intersectionPointColection, new IntPtr(), new IntPtr());
                if (intersectionPointColection.Count == 1)
                {
                    textPositionPoint = intersectionPointColection[0];
                }

                #endregion //Zoeken naar snijdingspunt userGeneretedLine - offsetarc

                double textAngle;

                #region Berekenen van de hoek die we moeten hebben voor de text

                if (isOnInsideOfArc)
                {
                    textAngle = _selectedAngle - ((90) * (Math.PI / 180));
                }
                else
                {
                    textAngle = _selectedAngle + ((90 + 180) * (Math.PI / 180));
                }
                if (_fliptexst)
                {
                    textAngle = textAngle + ((180) * (Math.PI / 180));
                }

                #endregion Berekenen van de hoek die we moeten hebben voor de text

                //Set the found base point and angle to our text

                //((DBText)this.Entity).HorizontalMode = TextHorizontalMode.TextCenter;

                //((DBText)this.Entity).HorizontalMode = TextHorizontalMode.TextCenter;
                //((DBText)this.Entity).VerticalMode = TextVerticalMode.TextBase;
                //((DBText)this.Entity).AlignmentPoint = textPositionPoint;
                //((DBText)this.Entity).Position = textPositionPoint;
                //((DBText)this.Entity).Rotation = textAngle;
                //((DBText)this.Entity).AdjustAlignment(acCurDb);

                ((MText)this.Entity).Attachment = AttachmentPoint.MiddleCenter;
                ((MText)this.Entity).Location = textPositionPoint;
                ((MText)this.Entity).Rotation = textAngle;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                if (myOffsetArc != null)
                    myOffsetArc.Dispose();
                if (userGeneretedLine != null)
                    userGeneretedLine.Dispose();
                if (intersectionPointColection != null)
                    intersectionPointColection.Dispose();
            }

            return true;
        }

        private bool updateLine()
        {
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            Line myOffsetLine = null;
            Line userGeneretedLine = null;
            Line myOffsetLine1 = null;
            Line myOffsetLine2 = null;
            Point3dCollection intersectionPointColection = null;
            double extraAngle = 0.0;

            try
            {
                #region Creat a imagenary [userGeneretedLine] line between the closestpoint on the line and the selectedpoint

                //find closest point to line from the currently selected point
                var clostespoint = _BaseLine.GetClosestPointTo(_selectedpoint, true);

                // if points are to close, dont update
                if (clostespoint.DistanceTo(_selectedpoint) < 0.001)
                    return true;

                // Create this new line
                userGeneretedLine = new Line(clostespoint, _selectedpoint);
                userGeneretedLine.SetDatabaseDefaults();

                #endregion Creat a imagenary [userGeneretedLine] line between the closestpoint on the line and the selectedpoint

                #region Get angle and length from this line

                _selectedLength = userGeneretedLine.Length;
                _selectedAngle = userGeneretedLine.Angle;

                #endregion Get angle and length from this line

                #region Nu we een lijn hebben die paralel op deze andere lijn staat, moeten we de offsetlijn zoeken

                //Er zij steeds 2 offsetlijnen gebruik steeds de offsetlijn die het dichtst bij het geselecteerde punt licht

                myOffsetLine1 = (Line)_BaseLine.GetOffsetCurves(_offset + _textHeight / 2)[0];
                myOffsetLine2 = (Line)_BaseLine.GetOffsetCurves(-(_offset + _textHeight / 2))[0];

                var distanceOffsetLine1 = myOffsetLine1.GetClosestPointTo(_selectedpoint, true).DistanceTo(_selectedpoint);
                var distanceOffsetLine2 = myOffsetLine2.GetClosestPointTo(_selectedpoint, true).DistanceTo(_selectedpoint);

                if (distanceOffsetLine1 < distanceOffsetLine2)
                {
                    myOffsetLine = myOffsetLine1;
                }
                else
                {
                    myOffsetLine = myOffsetLine2;
                    extraAngle = ((180) * (Math.PI / 180));
                }

                #endregion Nu we een lijn hebben die paralel op deze andere lijn staat, moeten we de offsetlijn zoeken

                #region Zoek het snijpunt waar de texst moet geplaatstworden  userGeneretedLine - offsetarc

                Point3d myTextPositionPoint = _selectedpoint;

                var myPlaneWCS = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));
                intersectionPointColection = new Point3dCollection();

                //userGeneretedLine.IntersectWith(myOffsetArc, Intersect.ExtendThis, intersectionPointColection, 0, 0); OBSOLETE CODE
                userGeneretedLine.IntersectWith(myOffsetLine, Intersect.ExtendThis, myPlaneWCS, intersectionPointColection, new IntPtr(), new IntPtr());
                if (intersectionPointColection.Count == 1)
                {
                    myTextPositionPoint = intersectionPointColection[0];
                }

                #endregion Zoek het snijpunt waar de texst moet geplaatstworden  userGeneretedLine - offsetarc

                double textAngle;

                #region Berekenen van de hoek die we moeten hebben voor de text

                textAngle = _selectedAngle + ((90 + 180) * (Math.PI / 180));
                if (_fliptexst)
                {
                    textAngle = textAngle + ((180) * (Math.PI / 180));
                }

                #endregion Berekenen van de hoek die we moeten hebben voor de text

                //Set the found base point and angle to our text

                //((DBText)this.Entity).HorizontalMode = TextHorizontalMode.TextCenter;

                //((DBText)this.Entity).HorizontalMode = TextHorizontalMode.TextCenter;
                //((DBText)this.Entity).VerticalMode = TextVerticalMode.TextVerticalMid;
                //((DBText)this.Entity).AlignmentPoint = textPositionPoint;
                //((DBText)this.Entity).Position = textPositionPoint;
                //((DBText)this.Entity).Rotation = textAngle + extraAngle;
                //((DBText)this.Entity).AdjustAlignment(acCurDb);

                ((MText)this.Entity).Attachment = AttachmentPoint.MiddleCenter;
                ((MText)this.Entity).Location = myTextPositionPoint;
                ((MText)this.Entity).Rotation = textAngle + extraAngle;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                if (userGeneretedLine != null)
                    userGeneretedLine.Dispose();
                if (intersectionPointColection != null)
                    intersectionPointColection.Dispose();
                if (myOffsetLine != null)
                    myOffsetLine.Dispose();
                if (myOffsetLine1 != null)
                    myOffsetLine1.Dispose();
                if (myOffsetLine2 != null)
                    myOffsetLine2.Dispose();
            }

            return true;
        }
    }
}