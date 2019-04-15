﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
using System.Windows.Shapes;

using II;
using II.Rhythm;
using II.Localization;

namespace II_Windows {
    /// <summary>
    /// Interaction logic for DeviceDefib.xaml
    /// </summary>
    public partial class DeviceDefib : Window {

        public enum Modes {
            DEFIB,
            SYNC,
            PACER
        };

        // Device settings
        public Modes Mode = Modes.DEFIB;
        public bool Charged = false;

        public int Energy = 200,
                    PacerEnergy = 0,
                    PacerRate = 80;

        int rowsTracings = 1,
            colsNumerics = 4;
        bool isFullscreen = false,
             isPaused = false;

        List<Controls.DefibTracing> listTracings = new List<Controls.DefibTracing> ();
        List<Controls.DefibNumeric> listNumerics = new List<Controls.DefibNumeric> ();

        Timer timerTracing = new Timer (),
              timerVitals = new Timer ();

        // Define WPF UI commands for binding
        private ICommand icToggleFullscreen, icPauseDevice, icCloseDevice, icExitProgram;
        public ICommand IC_ToggleFullscreen { get { return icToggleFullscreen; } }
        public ICommand IC_PauseDevice { get { return icPauseDevice; } }
        public ICommand IC_CloseDevice { get { return icCloseDevice; } }
        public ICommand IC_ExitProgram { get { return icExitProgram; } }


        public DeviceDefib () {
            InitializeComponent ();
            DataContext = this;

            InitInterface ();

            timerTracing.Interval = Waveforms.Draw_Refresh;
            App.Timer_Main.Tick += timerTracing.Process;
            timerTracing.Tick += OnTick_Tracing;
            timerTracing.Start ();

            timerVitals.Interval = 5000;
            App.Timer_Main.Tick += timerVitals.Process;
            timerVitals.Tick += OnTick_Vitals;
            timerVitals.Start ();

            OnLayoutChange ();
            OnTick_Vitals (null, new EventArgs ());
        }

        private void InitInterface () {
            // Initiate ICommands for KeyBindings
            icToggleFullscreen = new ActionCommand(() => ToggleFullscreen());
            icPauseDevice = new ActionCommand (() => TogglePause ());
            icCloseDevice = new ActionCommand (() => this.Close ());
            icExitProgram = new ActionCommand (() => App.Patient_Editor.RequestExit ());

            // Populate UI strings per language selection
            wdwDeviceDefib.Title = App.Language.Dictionary["DEFIB:WindowTitle"];
            menuDevice.Header = App.Language.Dictionary["MENU:MenuDeviceOptions"];
            menuPauseDevice.Header = App.Language.Dictionary["MENU:MenuPauseDevice"];
            menuAddNumeric.Header = App.Language.Dictionary["MENU:MenuAddNumeric"];
            menuAddTracing.Header = App.Language.Dictionary["MENU:MenuAddTracing"];
            menuToggleFullscreen.Header = App.Language.Dictionary["MENU:MenuToggleFullscreen"];
            menuCloseDevice.Header = App.Language.Dictionary["MENU:MenuCloseDevice"];
            menuExitProgram.Header = App.Language.Dictionary["MENU:MenuExitProgram"];

            btntxtDefib.Text = App.Language.Dictionary["DEFIB:Defibrillator"];
            txtEnergyAmount.Text = App.Language.Dictionary["DEFIB:EnergyAmount"];
            btntxtEnergyDecrease.Text = App.Language.Dictionary["DEFIB:Decrease"];
            btntxtEnergyIncrease.Text = App.Language.Dictionary["DEFIB:Increase"];
            btntxtCharge.Text = App.Language.Dictionary["DEFIB:Charge"];
            btntxtShock.Text = App.Language.Dictionary["DEFIB:Shock"];
            btntxtAnalyze.Text = App.Language.Dictionary["DEFIB:Analyze"];
            btntxtSync.Text = App.Language.Dictionary["DEFIB:Sync"];

            btntxtPacer.Text = App.Language.Dictionary["DEFIB:Pacer"];
            txtPaceRate.Text = App.Language.Dictionary["DEFIB:Rate"];
            btntxtPaceRateDecrease.Text = App.Language.Dictionary["DEFIB:Decrease"];
            btntxtPaceRateIncrease.Text = App.Language.Dictionary["DEFIB:Increase"];
            txtPaceEnergy.Text = App.Language.Dictionary["DEFIB:EnergyAmount"];
            btntxtPaceEnergyDecrease.Text = App.Language.Dictionary["DEFIB:Decrease"];
            btntxtPaceEnergyIncrease.Text = App.Language.Dictionary["DEFIB:Increase"];
            btntxtPacePause.Text = App.Language.Dictionary["DEFIB:Pause"];
        }

        public void Load_Process (string inc) {
            StringReader sRead = new StringReader (inc);
            List<string> numericTypes = new List<string> (),
                         tracingTypes = new List<string> ();

            try {
                string line;
                while ((line = sRead.ReadLine ()) != null) {
                    if (line.Contains (":")) {
                        string pName = line.Substring (0, line.IndexOf (':')),
                                pValue = line.Substring (line.IndexOf (':') + 1);
                        switch (pName) {
                            default: break;
                            case "rowsTracings": rowsTracings = int.Parse (pValue); break;
                            case "colsNumerics": colsNumerics = int.Parse (pValue); break;
                            case "isPaused": isPaused = bool.Parse (pValue); break;
                            case "isFullscreen": isFullscreen = bool.Parse (pValue); break;
                            case "numericTypes": numericTypes.AddRange (pValue.Split (',')); break;
                            case "tracingTypes": tracingTypes.AddRange (pValue.Split (',')); break;
                        }
                    }
                }
            } catch {
                sRead.Close ();
                return;
            }

            sRead.Close ();

            OnLayoutChange (numericTypes, tracingTypes);
        }

        public string Save () {
            StringBuilder sWrite = new StringBuilder ();

            sWrite.AppendLine (String.Format ("{0}:{1}", "rowsTracings", rowsTracings));
            sWrite.AppendLine (String.Format ("{0}:{1}", "colsNumerics", colsNumerics));
            sWrite.AppendLine (String.Format ("{0}:{1}", "isPaused", isPaused));
            sWrite.AppendLine (String.Format ("{0}:{1}", "isFullscreen", isFullscreen));

            List<string> numericTypes = new List<string> (),
                         tracingTypes = new List<string> ();

            listNumerics.ForEach (o => { numericTypes.Add (o.controlType.Value.ToString ()); });
            listTracings.ForEach (o => { tracingTypes.Add (o.wfStrip.Lead.Value.ToString ()); });
            sWrite.AppendLine (String.Format ("{0}:{1}", "numericTypes", string.Join (",", numericTypes)));
            sWrite.AppendLine (String.Format ("{0}:{1}", "tracingTypes", string.Join (",", tracingTypes)));

            return sWrite.ToString ();
        }

        private void ApplyFullScreen () {
            menuToggleFullscreen.IsChecked = isFullscreen;

            switch (isFullscreen) {
                default:
                case false:
                    wdwDeviceDefib.WindowStyle = WindowStyle.SingleBorderWindow;
                    wdwDeviceDefib.WindowState = WindowState.Normal;
                    wdwDeviceDefib.ResizeMode = ResizeMode.CanResize;
                    break;

                case true:
                    wdwDeviceDefib.WindowStyle = WindowStyle.None;
                    wdwDeviceDefib.WindowState = WindowState.Maximized;
                    wdwDeviceDefib.ResizeMode = ResizeMode.NoResize;
                    break;
            }
        }

        private void TogglePause () {
            isPaused = !isPaused;
            menuPauseDevice.IsChecked = isPaused;

            if (!isPaused)
                foreach (Controls.DefibTracing c in listTracings)
                    c.Unpause ();
        }

        private void ToggleFullscreen()
        {
            isFullscreen = !isFullscreen;
            ApplyFullScreen();
        }

        public void AddTracing () {
            rowsTracings += 1;
            OnLayoutChange ();
        }

        public void RemoveTracing (Controls.DefibTracing requestSender) {
            rowsTracings -= 1;
            listTracings.Remove (requestSender);
            OnLayoutChange ();
        }

        public void AddNumeric () {
            colsNumerics += 1;
            OnLayoutChange ();
        }

        public void RemoveNumeric (Controls.DefibNumeric requestSender) {
            colsNumerics-= 1;
            listNumerics.Remove (requestSender);
            OnLayoutChange ();
        }

        private void UpdateInterface () {
            listNumerics.Find(o => o.controlType.Value == Controls.DefibNumeric.ControlType.Values.DEFIB).UpdateVitals(this);
        }


        private void ButtonDefib_Click(object s, RoutedEventArgs e)
        {
            Mode = Modes.DEFIB;
            UpdateInterface();
        }
        private void ButtonEnergyDecrease_Click(object s, RoutedEventArgs e) {
            Energy = Utility.Clamp(Energy - 20, 0, 200);
            UpdateInterface();
        }
        private void ButtonEnergyIncrease_Click(object s, RoutedEventArgs e) {
            Energy = Utility.Clamp(Energy + 20, 0, 200);
            UpdateInterface();
        }
        private void ButtonCharge_Click(object s, RoutedEventArgs e) { throw new NotImplementedException(); }
        private void ButtonShock_Click(object s, RoutedEventArgs e) { throw new NotImplementedException(); }
        private void ButtonAnalyze_Click(object s, RoutedEventArgs e) { throw new NotImplementedException(); }
        private void ButtonSync_Click(object s, RoutedEventArgs e) {
            Mode = (Mode != Modes.SYNC ? Modes.SYNC : Modes.DEFIB);
            UpdateInterface();
        }

        private void ButtonPacer_Click(object s, RoutedEventArgs e) {
            Mode = (Mode != Modes.PACER ? Modes.PACER : Modes.DEFIB);
            UpdateInterface();
        }
        private void ButtonPaceRateDecrease_Click(object s, RoutedEventArgs e) {
            PacerRate = Utility.Clamp(PacerRate - 5, 0, 200);
            UpdateInterface();
        }
        private void ButtonPaceRateIncrease_Click(object s, RoutedEventArgs e) {
            PacerRate = Utility.Clamp(PacerRate + 5, 0, 200);
            UpdateInterface();
        }
        private void ButtonPaceEnergyDecrease_Click(object s, RoutedEventArgs e) {
            PacerEnergy = Utility.Clamp(PacerEnergy - 5, 0, 200);
            UpdateInterface();
        }
        private void ButtonPaceEnergyIncrease_Click(object s, RoutedEventArgs e) {
            PacerEnergy = Utility.Clamp(PacerEnergy + 5, 0, 200);
            UpdateInterface();
        }
        private void ButtonPacePause_Click(object s, RoutedEventArgs e) { throw new NotImplementedException(); }


        private void MenuClose_Click (object s, RoutedEventArgs e) => this.Close ();
        private void MenuExit_Click (object s, RoutedEventArgs e) => App.Patient_Editor.RequestExit ();
        private void MenuAddNumeric_Click (object s, RoutedEventArgs e) => AddNumeric ();
        private void MenuAddTracing_Click (object s, RoutedEventArgs e) => AddTracing ();
        private void MenuTogglePause_Click (object s, RoutedEventArgs e) => TogglePause ();

        private void MenuFullscreen_Click (object sender, RoutedEventArgs e) {
            isFullscreen = !isFullscreen;
            ApplyFullScreen ();
        }

        private void OnTick_Tracing (object sender, EventArgs e) {
            if (isPaused)
                return;

            foreach (Controls.DefibTracing c in listTracings) {
                c.Scroll ();
                c.Draw ();
            }
        }

        private void OnTick_Vitals (object sender, EventArgs e) {
            if (isPaused)
                return;

            foreach (Controls.DefibNumeric v in listNumerics)
                v.UpdateVitals (this);
        }

        private void OnLayoutChange (List<string> numericTypes = null, List<string> tracingTypes = null) {
            // If numericTypes or tracingTypes are not null... then we are loading a file; clear lNumerics and lTracings!
            if (numericTypes != null)
                listNumerics.Clear ();
            if (tracingTypes != null)
                listTracings.Clear ();

            // Set default numeric types to populate
            if (numericTypes == null || numericTypes.Count == 0)
                numericTypes = new List<string> (new string [] { "DEFIB", "ECG", "NIBP", "SPO2", "ETCO2", "ABP" });
            else if (numericTypes.Count < colsNumerics) {
                List<string> buffer = new List<string> (new string [] { "DEFIB", "ECG", "NIBP", "SPO2", "ETCO2", "ABP" });
                buffer.RemoveRange (0, numericTypes.Count);
                numericTypes.AddRange (buffer);
            }

            for (int i = listNumerics.Count; i < colsNumerics && i < numericTypes.Count; i++) {
                Controls.DefibNumeric newNum;
                newNum = new Controls.DefibNumeric ((Controls.DefibNumeric.ControlType.Values)Enum.Parse (typeof (Controls.DefibNumeric.ControlType.Values), numericTypes [i]));
                listNumerics.Add (newNum);
            }

            // Set default tracing types to populate
            if (tracingTypes == null || tracingTypes.Count == 0)
                tracingTypes = new List<string> (new string [] { "ECG_II", "SPO2", "ETCO2", "ABP" });
            else if (tracingTypes.Count < rowsTracings) {
                List<string> buffer = new List<string> (new string [] { "ECG_II", "SPO2", "ETCO2", "ABP" });
                buffer.RemoveRange (0, tracingTypes.Count);
                tracingTypes.AddRange (buffer);
            }

            for (int i = listTracings.Count; i < rowsTracings && i < tracingTypes.Count; i++) {
                Strip newStrip = new Strip (6f, (Leads.Values)Enum.Parse (typeof (Leads.Values), tracingTypes [i]));
                Controls.DefibTracing newTracing = new Controls.DefibTracing (newStrip);
                listTracings.Add (newTracing);
            }

            // Reset the UI container and repopulate with the UI elements
            gridNumerics.Children.Clear ();
            gridNumerics.ColumnDefinitions.Clear ();
            for (int i = 0; i < colsNumerics && i < listNumerics.Count; i++) {
                gridNumerics.ColumnDefinitions.Add(new ColumnDefinition());
                listNumerics [i].SetValue (Grid.ColumnProperty, i);
                gridNumerics.Children.Add (listNumerics [i]);
            }

            gridTracings.Children.Clear ();
            gridTracings.RowDefinitions.Clear ();
            for (int i = 0; i < rowsTracings && i < listTracings.Count; i++) {
                gridTracings.RowDefinitions.Add (new RowDefinition ());
                listTracings [i].SetValue (Grid.RowProperty, i);
                gridTracings.Children.Add (listTracings [i]);
            }
        }

        public void OnPatientEvent (object sender, Patient.PatientEvent_Args e) {
            switch (e.EventType) {
                default: break;
                case Patient.PatientEvent_Args.EventTypes.Vitals_Change:
                    foreach (Controls.DefibTracing c in listTracings) {
                        c.ClearFuture ();
                        c.Add_Beat__Cardiac_Baseline (App.Patient);
                    }
                    foreach (Controls.DefibNumeric n in listNumerics)
                        n.UpdateVitals (this);
                    break;

                case Patient.PatientEvent_Args.EventTypes.Cardiac_Baseline:
                    foreach (Controls.DefibTracing c in listTracings)
                        c.Add_Beat__Cardiac_Baseline (App.Patient);
                    break;

                case Patient.PatientEvent_Args.EventTypes.Cardiac_Atrial:
                    foreach (Controls.DefibTracing c in listTracings)
                        c.Add_Beat__Cardiac_Atrial (App.Patient);
                    break;

                case Patient.PatientEvent_Args.EventTypes.Cardiac_Ventricular:
                    foreach (Controls.DefibTracing c in listTracings)
                        c.Add_Beat__Cardiac_Ventricular (App.Patient);
                    break;

                case Patient.PatientEvent_Args.EventTypes.Respiratory_Baseline:
                    foreach (Controls.DefibTracing c in listTracings)
                        c.Add_Beat__Respiratory_Baseline (App.Patient);
                    break;

                case Patient.PatientEvent_Args.EventTypes.Respiratory_Inspiration:
                    foreach (Controls.DefibTracing c in listTracings)
                        c.Add_Beat__Respiratory_Inspiration (App.Patient);
                    break;

                case Patient.PatientEvent_Args.EventTypes.Respiratory_Expiration:
                    foreach (Controls.DefibTracing c in listTracings)
                        c.Add_Beat__Respiratory_Expiration (App.Patient);
                    break;
            }
        }

        private void OnFormResize (object sender, RoutedEventArgs e) => OnLayoutChange ();
    }
}