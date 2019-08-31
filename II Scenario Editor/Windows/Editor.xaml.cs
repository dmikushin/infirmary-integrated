﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using II;
using II.Scenario_Editor.Controls;

namespace II.Scenario_Editor {

    public partial class Editor : Window {

        // Variables for capturing mouse and dragging UI elements
        private bool mouseCaptured = false;

        private double xShape, yShape,
            xCanvas, yCanvas;

        private Canvas canvasDesigner;
        private ItemStep selStep;
        private ItemStep.UIEProgression selProgression;

        private int indexStep = -1,
            indexProgression = -1;

        private Patient copiedPatient;

        private List<ItemStep> Steps = new List<ItemStep> ();

        public Editor () {
            InitializeComponent ();

            canvasDesigner = cnvsDesigner;
        }

        private void setPropertyView () {
            if (selStep == null)
                return;

            // Populate enum string lists for readable display
            List<string> cardiacRhythms = new List<string> (),
                respiratoryRhythms = new List<string> (),
                pulmonaryRhythms = new List<string> (),
                cardiacAxes = new List<string> (),
                intensityScale = new List<string> (),
                fetalHeartRhythms = new List<string> ();

            foreach (Cardiac_Rhythms.Values v in Enum.GetValues (typeof (Cardiac_Rhythms.Values)))
                cardiacRhythms.Add (App.Language.Dictionary [Cardiac_Rhythms.LookupString (v)]);

            foreach (Respiratory_Rhythms.Values v in Enum.GetValues (typeof (Respiratory_Rhythms.Values)))
                respiratoryRhythms.Add (App.Language.Dictionary [Respiratory_Rhythms.LookupString (v)]);

            foreach (PulmonaryArtery_Rhythms.Values v in Enum.GetValues (typeof (PulmonaryArtery_Rhythms.Values)))
                pulmonaryRhythms.Add (App.Language.Dictionary [PulmonaryArtery_Rhythms.LookupString (v)]);

            foreach (Cardiac_Axes.Values v in Enum.GetValues (typeof (Cardiac_Axes.Values)))
                cardiacAxes.Add (App.Language.Dictionary [Cardiac_Axes.LookupString (v)]);

            // Initiate controls for editing Patient values
            pstrName.Init (PropertyString.Keys.Name, selStep.Step.Name ?? "");
            pstrName.PropertyChanged += updateProperty;

            pstrDescription.Init (PropertyString.Keys.Description, selStep.Step.Description ?? "");
            pstrDescription.PropertyChanged += updateProperty;

            pintHR.Init (PropertyInt.Keys.HR, selStep.Patient.VS_Settings.HR, 5, 0, 500);
            pintHR.PropertyChanged += updateProperty;

            pbpNBP.Init (PropertyBP.Keys.NSBP,
                selStep.Patient.VS_Settings.NSBP, selStep.Patient.VS_Settings.NDBP,
                5, 0, 300,
                5, 0, 200);
            pbpNBP.PropertyChanged += updateProperty;

            pintRR.Init (PropertyInt.Keys.RR, selStep.Patient.VS_Settings.RR, 2, 0, 100);
            pintRR.PropertyChanged += updateProperty;

            pintSPO2.Init (PropertyInt.Keys.SPO2, selStep.Patient.VS_Settings.SPO2, 2, 0, 100);
            pintSPO2.PropertyChanged += updateProperty;

            pdblT.Init (PropertyDouble.Keys.T, selStep.Patient.VS_Settings.T, 0.2, 0, 100);
            pdblT.PropertyChanged += updateProperty;

            penmCardiacRhythms.Init (PropertyEnum.Keys.Cardiac_Rhythms,
                Enum.GetNames (typeof (Cardiac_Rhythms.Values)),
                cardiacRhythms, (int)selStep.Patient.Cardiac_Rhythm.Value);
            penmCardiacRhythms.PropertyChanged += updateProperty;
            penmCardiacRhythms.PropertyChanged += updateCardiacRhythm;

            penmRespiratoryRhythms.Init (PropertyEnum.Keys.Respiratory_Rhythms,
                Enum.GetNames (typeof (Respiratory_Rhythms.Values)),
                respiratoryRhythms, (int)selStep.Patient.Respiratory_Rhythm.Value);
            penmRespiratoryRhythms.PropertyChanged += updateProperty;
            penmRespiratoryRhythms.PropertyChanged += updateRespiratoryRhythm;

            pintETCO2.Init (PropertyInt.Keys.ETCO2, selStep.Patient.VS_Settings.ETCO2, 2, 0, 100);
            pintETCO2.PropertyChanged += updateProperty;

            pintCVP.Init (PropertyInt.Keys.CVP, selStep.Patient.VS_Settings.CVP, 1, -100, 100);
            pintCVP.PropertyChanged += updateProperty;

            pbpABP.Init (PropertyBP.Keys.ASBP,
                selStep.Patient.VS_Settings.ASBP, selStep.Patient.VS_Settings.ADBP,
                5, 0, 300,
                5, 0, 200);
            pbpABP.PropertyChanged += updateProperty;

            penmPACatheterRhythm.Init (PropertyEnum.Keys.PACatheter_Rhythms,
                Enum.GetNames (typeof (PulmonaryArtery_Rhythms.Values)),
                pulmonaryRhythms, (int)selStep.Patient.PulmonaryArtery_Placement.Value);
            penmPACatheterRhythm.PropertyChanged += updateProperty;
            penmPACatheterRhythm.PropertyChanged += updatePACatheterRhythm;

            pbpPBP.Init (PropertyBP.Keys.PSP,
                selStep.Patient.VS_Settings.PSP, selStep.Patient.VS_Settings.PDP,
                5, 0, 200,
                5, 0, 200);
            pbpPBP.PropertyChanged += updateProperty;

            pintICP.Init (PropertyInt.Keys.ICP, selStep.Patient.VS_Settings.ICP, 1, -100, 100);
            pintICP.PropertyChanged += updateProperty;

            pintIAP.Init (PropertyInt.Keys.IAP, selStep.Patient.VS_Settings.IAP, 1, -100, 100);
            pintIAP.PropertyChanged += updateProperty;

            pchkMechanicallyVentilated.Init (PropertyCheck.Keys.MechanicallyVentilated, selStep.Patient.Mechanically_Ventilated);
            pchkMechanicallyVentilated.PropertyChanged += updateProperty;

            pdblInspiratoryRatio.Init (PropertyFloat.Keys.RRInspiratoryRatio, selStep.Patient.VS_Settings.RR_IE_I, 0.1, 0.1, 10);
            pdblInspiratoryRatio.PropertyChanged += updateProperty;

            pdblExpiratoryRatio.Init (PropertyFloat.Keys.RRExpiratoryRatio, selStep.Patient.VS_Settings.RR_IE_E, 0.1, 0.1, 10);
            pdblExpiratoryRatio.PropertyChanged += updateProperty;

            pintPacemakerThreshold.Init (PropertyInt.Keys.PacemakerThreshold, selStep.Patient.Pacemaker_Threshold, 5, 0, 200);
            pintPacemakerThreshold.PropertyChanged += updateProperty;

            pchkPulsusParadoxus.Init (PropertyCheck.Keys.PulsusParadoxus, selStep.Patient.Pulsus_Paradoxus);
            pchkPulsusParadoxus.PropertyChanged += updateProperty;

            pchkPulsusAlternans.Init (PropertyCheck.Keys.PulsusAlternans, selStep.Patient.Pulsus_Alternans);
            pchkPulsusAlternans.PropertyChanged += updateProperty;

            penmCardiacAxis.Init (PropertyEnum.Keys.Cardiac_Axis,
                Enum.GetNames (typeof (Cardiac_Axes.Values)),
                cardiacAxes, (int)selStep.Patient.Cardiac_Axis.Value);
            penmCardiacAxis.PropertyChanged += updateProperty;

            pecgSTSegment.Init (PropertyECGSegment.Keys.STElevation, selStep.Patient.ST_Elevation);
            pecgSTSegment.PropertyChanged += updateProperty;

            pecgTWave.Init (PropertyECGSegment.Keys.TWave, selStep.Patient.T_Elevation);
            pecgTWave.PropertyChanged += updateProperty;

            pintProgressFrom.Init (PropertyInt.Keys.ProgressFrom, selStep.Step.ProgressFrom, 1, -1, 1000);
            pintProgressFrom.PropertyChanged += updateProperty;

            pintProgressTo.Init (PropertyInt.Keys.ProgressTo, selStep.Step.ProgressTo, 1, -1, 1000);
            pintProgressTo.PropertyChanged += updateProperty;

            pintProgressTimer.Init (PropertyInt.Keys.ProgressTimer, selStep.Step.ProgressTimer, 1, -1, 1000);
            pintProgressTimer.PropertyChanged += updateProperty;
        }

        private void updatePropertyView () {
            if (selStep == null)
                return;

            // Update all controls with Patient values
            pstrName.Set (selStep.Step.Name ?? "");
            pstrDescription.Set (selStep.Step.Description ?? "");
            pintHR.Set (selStep.Patient.VS_Settings.HR);
            pbpNBP.Set (selStep.Patient.VS_Settings.NSBP, selStep.Patient.VS_Settings.NDBP);
            pintRR.Set (selStep.Patient.VS_Settings.RR);
            pintSPO2.Set (selStep.Patient.VS_Settings.SPO2);
            pdblT.Set (selStep.Patient.VS_Settings.T);
            penmCardiacRhythms.Set ((int)selStep.Patient.Cardiac_Rhythm.Value);
            penmRespiratoryRhythms.Set ((int)selStep.Patient.Respiratory_Rhythm.Value);
            pintETCO2.Set (selStep.Patient.VS_Settings.ETCO2);
            pintCVP.Set (selStep.Patient.VS_Settings.CVP);
            pbpABP.Set (selStep.Patient.VS_Settings.ASBP, selStep.Patient.VS_Settings.ADBP);
            pbpPBP.Set (selStep.Patient.VS_Settings.PSP, selStep.Patient.VS_Settings.PDP);
            pintICP.Set (selStep.Patient.VS_Settings.ICP);
            pintIAP.Set (selStep.Patient.VS_Settings.IAP);
            pchkMechanicallyVentilated.Set (selStep.Patient.Mechanically_Ventilated);
            pdblInspiratoryRatio.Set (selStep.Patient.VS_Settings.RR_IE_I);
            pdblExpiratoryRatio.Set (selStep.Patient.VS_Settings.RR_IE_E);
            pintPacemakerThreshold.Set (selStep.Patient.Pacemaker_Threshold);
            pchkPulsusParadoxus.Set (selStep.Patient.Pulsus_Paradoxus);
            pchkPulsusAlternans.Set (selStep.Patient.Pulsus_Alternans);
            penmCardiacAxis.Set ((int)selStep.Patient.Cardiac_Axis.Value);
            pecgSTSegment.Set (selStep.Patient.ST_Elevation);
            pecgTWave.Set (selStep.Patient.T_Elevation);

            // Update progression controls with values
            pintProgressFrom.Set (selStep.Step.ProgressFrom);
            pintProgressTo.Set (selStep.Step.ProgressTo);
            pintProgressTimer.Set (selStep.Step.ProgressTimer);
        }

        private void updateProperty (object sender, PropertyString.PropertyStringEventArgs e) {
            switch (e.Key) {
                default: break;
                case PropertyString.Keys.Name:
                    selStep.SetName (e.Value);
                    break;

                case PropertyString.Keys.Description: selStep.Step.Description = e.Value; break;
            }
        }

        private void updateProperty (object sender, PropertyInt.PropertyIntEventArgs e) {
            switch (e.Key) {
                default: break;
                case PropertyInt.Keys.HR: selStep.Patient.HR = e.Value; break;
                case PropertyInt.Keys.RR: selStep.Patient.RR = e.Value; break;
                case PropertyInt.Keys.ETCO2: selStep.Patient.ETCO2 = e.Value; break;
                case PropertyInt.Keys.SPO2: selStep.Patient.SPO2 = e.Value; break;
                case PropertyInt.Keys.CVP: selStep.Patient.CVP = e.Value; break;
                case PropertyInt.Keys.ICP: selStep.Patient.ICP = e.Value; break;
                case PropertyInt.Keys.IAP: selStep.Patient.IAP = e.Value; break;
                case PropertyInt.Keys.PacemakerThreshold: selStep.Patient.Pacemaker_Threshold = e.Value; break;

                case PropertyInt.Keys.ProgressFrom: selStep.Step.ProgressFrom = e.Value; break;
                case PropertyInt.Keys.ProgressTo: selStep.Step.ProgressTo = e.Value; break;
                case PropertyInt.Keys.ProgressTimer: selStep.Step.ProgressTimer = e.Value; break;
            }
        }

        private void updateProperty (object sender, PropertyDouble.PropertyDoubleEventArgs e) {
            switch (e.Key) {
                default: break;
                case PropertyDouble.Keys.T: selStep.Patient.T = e.Value; break;
            }
        }

        private void updateProperty (object sender, PropertyFloat.PropertyFloatEventArgs e) {
            switch (e.Key) {
                default: break;
                case PropertyFloat.Keys.RRInspiratoryRatio: selStep.Patient.RR_IE_I = e.Value; break;
                case PropertyFloat.Keys.RRExpiratoryRatio: selStep.Patient.RR_IE_E = e.Value; break;
            }
        }

        private void updateProperty (object sender, PropertyBP.PropertyIntEventArgs e) {
            switch (e.Key) {
                default: break;
                case PropertyBP.Keys.NSBP: selStep.Patient.NSBP = e.Value; break;
                case PropertyBP.Keys.NDBP: selStep.Patient.NDBP = e.Value; break;
                case PropertyBP.Keys.NMAP: selStep.Patient.NMAP = e.Value; break;
                case PropertyBP.Keys.ASBP: selStep.Patient.ASBP = e.Value; break;
                case PropertyBP.Keys.ADBP: selStep.Patient.ADBP = e.Value; break;
                case PropertyBP.Keys.AMAP: selStep.Patient.AMAP = e.Value; break;
                case PropertyBP.Keys.PSP: selStep.Patient.PSP = e.Value; break;
                case PropertyBP.Keys.PDP: selStep.Patient.PDP = e.Value; break;
                case PropertyBP.Keys.PMP: selStep.Patient.PMP = e.Value; break;
            }
        }

        private void updateProperty (object sender, PropertyEnum.PropertyEnumEventArgs e) {
            switch (e.Key) {
                default: break;

                case PropertyEnum.Keys.Cardiac_Axis:
                    selStep.Patient.Cardiac_Axis.Value = (Cardiac_Axes.Values)Enum.Parse (typeof (Cardiac_Axes.Values), e.Value);
                    break;

                case PropertyEnum.Keys.Cardiac_Rhythms:
                    selStep.Patient.Cardiac_Rhythm.Value = (Cardiac_Rhythms.Values)Enum.Parse (typeof (Cardiac_Rhythms.Values), e.Value);
                    break;

                case PropertyEnum.Keys.Respiratory_Rhythms:
                    selStep.Patient.Respiratory_Rhythm.Value = (Respiratory_Rhythms.Values)Enum.Parse (typeof (Respiratory_Rhythms.Values), e.Value);
                    break;

                case PropertyEnum.Keys.PACatheter_Rhythms:
                    selStep.Patient.PulmonaryArtery_Placement.Value = (PulmonaryArtery_Rhythms.Values)Enum.Parse (typeof (PulmonaryArtery_Rhythms.Values), e.Value);
                    break;
            }
        }

        private void updateProperty (object sender, PropertyCheck.PropertyCheckEventArgs e) {
            switch (e.Key) {
                default: break;
                case PropertyCheck.Keys.PulsusParadoxus: selStep.Patient.Pulsus_Paradoxus = e.Value; break;
                case PropertyCheck.Keys.PulsusAlternans: selStep.Patient.Pulsus_Alternans = e.Value; break;
                case PropertyCheck.Keys.MechanicallyVentilated: selStep.Patient.Mechanically_Ventilated = e.Value; break;
            }
        }

        private void updateProperty (object sender, PropertyECGSegment.PropertyECGEventArgs e) {
            switch (e.Key) {
                default: break;
                case PropertyECGSegment.Keys.STElevation: selStep.Patient.ST_Elevation = e.Values; break;
                case PropertyECGSegment.Keys.TWave: selStep.Patient.T_Elevation = e.Values; break;
            }
        }

        private void updateCardiacRhythm (object sender, PropertyEnum.PropertyEnumEventArgs e) {
            if (!chkClampVitals.IsChecked ?? false || selStep == null)
                return;

            Patient p = ((ItemStep)selStep).Patient;

            Cardiac_Rhythms.Default_Vitals v = Cardiac_Rhythms.DefaultVitals (
                (Cardiac_Rhythms.Values)Enum.Parse (typeof (Cardiac_Rhythms.Values), e.Value));

            p.HR = (int)Utility.Clamp ((double)p.VS_Settings.HR, v.HRMin, v.HRMax);
            p.RR = (int)Utility.Clamp ((double)p.VS_Settings.RR, v.RRMin, v.RRMax);
            p.SPO2 = (int)Utility.Clamp ((double)p.VS_Settings.SPO2, v.SPO2Min, v.SPO2Max);
            p.ETCO2 = (int)Utility.Clamp ((double)p.VS_Settings.ETCO2, v.ETCO2Min, v.ETCO2Max);
            p.NSBP = (int)Utility.Clamp ((double)p.VS_Settings.NSBP, v.SBPMin, v.SBPMax);
            p.NDBP = (int)Utility.Clamp ((double)p.VS_Settings.NDBP, v.DBPMin, v.DBPMax);
            p.ASBP = (int)Utility.Clamp ((double)p.VS_Settings.ASBP, v.SBPMin, v.SBPMax);
            p.ADBP = (int)Utility.Clamp ((double)p.VS_Settings.ADBP, v.DBPMin, v.DBPMax);
            p.PSP = (int)Utility.Clamp ((double)p.VS_Settings.PSP, v.PSPMin, v.PSPMax);
            p.PDP = (int)Utility.Clamp ((double)p.VS_Settings.PDP, v.PDPMin, v.PDPMax);

            updatePropertyView ();
        }

        private void updateRespiratoryRhythm (object sender, PropertyEnum.PropertyEnumEventArgs e) {
            if (!chkClampVitals.IsChecked ?? false || selStep == null)
                return;

            Patient p = ((ItemStep)selStep).Patient;

            Respiratory_Rhythms.Default_Vitals v = Respiratory_Rhythms.DefaultVitals (
                (Respiratory_Rhythms.Values)Enum.Parse (typeof (Respiratory_Rhythms.Values), e.Value));

            p.RR = (int)Utility.Clamp ((double)p.RR, v.RRMin, v.RRMax);
            p.RR_IE_I = (int)Utility.Clamp ((double)p.RR_IE_I, v.RR_IE_I_Min, v.RR_IE_I_Max);
            p.RR_IE_E = (int)Utility.Clamp ((double)p.RR_IE_E, v.RR_IE_E_Min, v.RR_IE_E_Max);

            updatePropertyView ();
        }

        private void updatePACatheterRhythm (object sender, PropertyEnum.PropertyEnumEventArgs e) {
            if (selStep == null)
                return;

            Patient p = ((ItemStep)selStep).Patient;

            PulmonaryArtery_Rhythms.Default_Vitals v = PulmonaryArtery_Rhythms.DefaultVitals (
                (PulmonaryArtery_Rhythms.Values)Enum.Parse (typeof (PulmonaryArtery_Rhythms.Values), e.Value));

            p.PSP = (int)Utility.Clamp ((double)p.PSP, v.PSPMin, v.PSPMax);
            p.PDP = (int)Utility.Clamp ((double)p.PDP, v.PDPMin, v.PDPMax);

            updatePropertyView ();
        }

        private void addStep (ItemStep ist = null) {
            if (ist == null)
                ist = new ItemStep ();

            // Init ItemStep
            ist.Init ();

            ist.IStep.MouseLeftButtonDown += IStep_MouseLeftButtonDown;
            ist.IStep.MouseLeftButtonUp += IStep_MouseLeftButtonUp;
            ist.IStep.MouseMove += IStep_MouseMove;

            ist.IProgression.MouseLeftButtonDown += IProgression_MouseLeftButtonDown;

            // Add to lists and display elements
            Steps.Add (ist);
            canvasDesigner.Children.Add (ist);

            // Set positions in visual space
            Canvas.SetLeft (ist, (cnvsDesigner.ActualWidth / 2) - (ist.Width / 2));
            Canvas.SetTop (ist, (cnvsDesigner.ActualHeight / 2) - (ist.Height / 2));

            // Select the added step, give a default name by its index
            selStep = ist;
            indexStep = Steps.FindIndex (o => { return o == selStep; });
            ist.SetName (indexStep.ToString ("000"));

            // Refresh the Properties View with the newly selected step
            setPropertyView ();

            expStepProperty.IsExpanded = true;
            expProgressionProperty.IsExpanded = true;
        }

        private void addProgression (ItemStep stepFrom, ItemStep stepTo) {
            //throw new NotImplementedException ();
            if (stepFrom == stepTo)
                return;

            int indexFrom = Steps.FindIndex (o => { return o == stepFrom; });
            int indexTo = Steps.FindIndex (o => { return o == stepTo; });

            if (stepFrom.Step.ProgressTo < 0)
                stepFrom.Step.ProgressTo = indexTo;

            if (stepTo.Step.ProgressFrom < 0)
                stepTo.Step.ProgressFrom = indexFrom;

            setPropertyView ();

            expStepProperty.IsExpanded = false;
            expProgressionProperty.IsExpanded = true;
        }

        private void ButtonAddStep_Click (object sender, RoutedEventArgs e)
            => addStep ();

        private void ButtonDuplicateStep_Click (object sender, RoutedEventArgs e) {
            if (selStep == null)
                return;

            addStep (((ItemStep)selStep).Duplicate ());
        }

        private void ButtonEditProgressions_Click (object sender, RoutedEventArgs e) {
            throw new NotImplementedException ();
        }

        private void BtnCopyPatient_Click (object sender, RoutedEventArgs e) {
            if (selStep == null)
                return;

            copiedPatient = new Patient ();
            copiedPatient.Load_Process (selStep.Patient.Save ());
        }

        private void BtnPastePatient_Click (object sender, RoutedEventArgs e) {
            if (selStep == null)
                return;

            if (copiedPatient != null) {
                selStep.Patient.Load_Process (copiedPatient.Save ());
            }

            updatePropertyView ();
        }

        private void ChkProgressTimer_Click (object sender, RoutedEventArgs e) {
            pintProgressTimer.IsEnabled = chkProgressTimer.IsChecked ?? false;
            if (chkProgressTimer.IsChecked ?? false == false)
                pintProgressTimer.numValue.Value = -1;
        }

        private void MenuItemExit_Click (object sender, RoutedEventArgs e)
            => Application.Current.Shutdown ();

        private void MenuItemAbout_Click (object sender, RoutedEventArgs e) {
            About dlgAbout = new About ();
            dlgAbout.ShowDialog ();
        }

        private void IStep_MouseLeftButtonDown (object sender, MouseButtonEventArgs e) {
            selStep = ((ItemStep.UIEStep)sender).ItemStep;

            Mouse.Capture (sender as ItemStep.UIEStep);
            mouseCaptured = true;

            xShape = selStep.Left;
            yShape = selStep.Top;
            xCanvas = e.GetPosition (LayoutRoot).X;
            yCanvas = e.GetPosition (LayoutRoot).Y;

            updatePropertyView ();

            expStepProperty.IsExpanded = true;
            expProgressionProperty.IsExpanded = true;
        }

        private void IStep_MouseLeftButtonUp (object sender, MouseButtonEventArgs e) {
            Mouse.Capture (null);
            mouseCaptured = false;
        }

        private void IStep_MouseMove (object sender, MouseEventArgs e) {
            if (mouseCaptured) {
                double x = e.GetPosition (LayoutRoot).X;
                double y = e.GetPosition (LayoutRoot).Y;
                xShape += x - xCanvas;
                xCanvas = x;
                yShape += y - yCanvas;
                yCanvas = y;

                ItemStep istep = ((ItemStep.UIEStep)sender).ItemStep;
                Canvas.SetLeft (selStep, Utility.Clamp (xShape, 0, canvasDesigner.ActualWidth - istep.ActualWidth));
                Canvas.SetTop (selStep, Utility.Clamp (yShape, 0, canvasDesigner.ActualHeight - istep.ActualHeight));
            }
        }

        private void IProgression_MouseLeftButtonDown (object sender, MouseButtonEventArgs e) {
            selStep = ((ItemStep.UIEProgression)sender).ItemStep;
            selProgression = (ItemStep.UIEProgression)sender;

            updatePropertyView ();

            expStepProperty.IsExpanded = false;
            expProgressionProperty.IsExpanded = true;
        }

        protected override void OnMouseLeftButtonUp (MouseButtonEventArgs e) {
            base.OnMouseLeftButtonUp (e);

            if (selProgression != null) {
                if (Mouse.DirectlyOver is ItemStep.UIEProgression) {
                    addProgression (selStep, ((ItemStep.UIEProgression)Mouse.DirectlyOver).ItemStep);
                } else if (Mouse.DirectlyOver is ItemStep.UIEStep)
                    addProgression (selStep, ((ItemStep.UIEStep)Mouse.DirectlyOver).ItemStep);
                else if (Mouse.DirectlyOver is ItemStep)
                    addProgression (selStep, (ItemStep)Mouse.DirectlyOver);
            }

            selProgression = null;
        }
    }
}