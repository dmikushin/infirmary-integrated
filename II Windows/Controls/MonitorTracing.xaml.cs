﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using II;
using II.Localization;
using II.Rhythm;

namespace II_Windows.Controls {

    /// <summary>
    /// Interaction logic for Tracing.xaml
    /// </summary>
    public partial class MonitorTracing : UserControl {
        public Strip Strip;
        public Lead Lead { get { return Strip.Lead; } }

        /* Drawing variables, offsets and multipliers */
        private Brush tracingBrush = Brushes.Black;
        private Brush referenceBrush = Brushes.DarkGray;

        private StreamGeometry drawGeometry;
        private StreamGeometryContext drawContext;
        private int drawXOffset, drawYOffset;
        private double drawXMultiplier, drawYMultiplier;

        private MenuItem menuZeroTransducer;
        private MenuItem menuToggleAutoScale;

        public MonitorTracing (Strip strip) {
            InitializeComponent ();
            DataContext = this;

            Strip = strip;

            InitInterface ();
            UpdateInterface (null, null);
        }

        private void InitInterface () {

            // Populate UI strings per language selection
            Language.Values l = App.Language.Value;

            // Context Menu (right-click menu!)
            ContextMenu contextMenu = new ContextMenu ();
            canvasTracing.ContextMenu = contextMenu;
            lblLead.ContextMenu = contextMenu;

            menuZeroTransducer = new MenuItem ();
            menuZeroTransducer.Header = App.Language.Localize ("MENU:MenuZeroTransducer");
            menuZeroTransducer.Click += MenuZeroTransducer_Click;
            contextMenu.Items.Add (menuZeroTransducer);

            contextMenu.Items.Add (new Separator ());

            MenuItem menuAddTracing = new MenuItem ();
            menuAddTracing.Header = App.Language.Localize ("MENU:MenuAddTracing");
            menuAddTracing.Click += MenuAddTracing_Click;
            contextMenu.Items.Add (menuAddTracing);

            MenuItem menuRemoveTracing = new MenuItem ();
            menuRemoveTracing.Header = App.Language.Localize ("MENU:MenuRemoveTracing");
            menuRemoveTracing.Click += MenuRemoveTracing_Click;
            contextMenu.Items.Add (menuRemoveTracing);

            contextMenu.Items.Add (new Separator ());

            MenuItem menuIncreaseAmplitude = new MenuItem ();
            menuIncreaseAmplitude.Header = App.Language.Localize ("MENU:IncreaseAmplitude");
            menuIncreaseAmplitude.Click += MenuIncreaseAmplitude_Click;
            contextMenu.Items.Add (menuIncreaseAmplitude);

            MenuItem menuDecreaseAmplitude = new MenuItem ();
            menuDecreaseAmplitude.Header = App.Language.Localize ("MENU:DecreaseAmplitude");
            menuDecreaseAmplitude.Click += MenuDecreaseAmplitude_Click;
            contextMenu.Items.Add (menuDecreaseAmplitude);

            contextMenu.Items.Add (new Separator ());

            menuToggleAutoScale = new MenuItem ();
            menuToggleAutoScale.Header = App.Language.Localize ("MENU:ToggleAutoScaling");
            menuToggleAutoScale.Click += MenuToggleAutoScale_Click;
            contextMenu.Items.Add (menuToggleAutoScale);

            contextMenu.Items.Add (new Separator ());

            MenuItem menuSelectInput = new MenuItem (),
                     menuECGLeads = new MenuItem ();
            menuSelectInput.Header = App.Language.Localize ("MENU:MenuSelectInputSource");
            menuECGLeads.Header = App.Language.Localize ("TRACING:ECG");
            menuSelectInput.Items.Add (menuECGLeads);

            foreach (Lead.Values v in Enum.GetValues (typeof (Lead.Values))) {

                // Only include certain leads- e.g. bedside monitors don't interface with IABP or EFM
                string el = v.ToString ();
                if (!el.StartsWith ("ECG") && el != "SPO2" && el != "RR" && el != "ETCO2"
                    && el != "CVP" && el != "ABP" && el != "PA"
                    && el != "ICP" && el != "IAP")
                    continue;

                MenuItem mi = new MenuItem ();
                mi.Header = App.Language.Localize (Lead.LookupString (v));
                mi.Name = v.ToString ();
                mi.Click += MenuSelectInputSource;
                if (mi.Name.StartsWith ("ECG"))
                    menuECGLeads.Items.Add (mi);
                else
                    menuSelectInput.Items.Add (mi);
            }

            contextMenu.Items.Add (menuSelectInput);
        }

        private void UpdateInterface (object sender, SizeChangedEventArgs e) {
            switch (Lead.Value) {
                default: tracingBrush = Brushes.Green; break;
                case Lead.Values.SPO2: tracingBrush = Brushes.Orange; break;
                case Lead.Values.RR: tracingBrush = Brushes.Salmon; break;
                case Lead.Values.ETCO2: tracingBrush = Brushes.Aqua; break;
                case Lead.Values.ABP: tracingBrush = Brushes.Red; break;
                case Lead.Values.CVP: tracingBrush = Brushes.Blue; break;
                case Lead.Values.PA: tracingBrush = Brushes.Yellow; break;
                case Lead.Values.ICP: tracingBrush = Brushes.Khaki; break;
                case Lead.Values.IAP: tracingBrush = Brushes.Aquamarine; break;
                case Lead.Values.IABP: tracingBrush = Brushes.SkyBlue; break;
            }

            borderTracing.BorderBrush = tracingBrush;

            lblLead.Foreground = tracingBrush;
            lblLead.Content = App.Language.Localize (Lead.LookupString (Lead.Value));

            menuZeroTransducer.IsEnabled = Strip.Lead.IsTransduced ();
            menuToggleAutoScale.IsEnabled = Strip.CanScale;

            if (Strip.CanScale) {
                lblScaleAuto.Foreground = tracingBrush;
                lblScaleMin.Foreground = tracingBrush;
                lblScaleMax.Foreground = tracingBrush;

                lblScaleAuto.Content = Strip.ScaleAuto
                    ? App.Language.Localize ("TRACING:Auto")
                    : App.Language.Localize ("TRACING:Fixed");
                lblScaleMin.Content = Strip.ScaleMin.ToString ();
                lblScaleMax.Content = Strip.ScaleMax.ToString ();
            }

            CalculateOffsets ();
        }

        public void UpdateScale () {
            if (Strip.CanScale) {
                lblScaleMin.Foreground = tracingBrush;
                lblScaleMax.Foreground = tracingBrush;

                lblScaleMin.Content = Strip.ScaleMin.ToString ();
                lblScaleMax.Content = Strip.ScaleMax.ToString ();
            }
        }

        public void CalculateOffsets ()
            => Tracing.CalculateOffsets (Strip, canvasTracing.ActualWidth, canvasTracing.ActualHeight,
                ref drawXOffset, ref drawYOffset, ref drawXMultiplier, ref drawYMultiplier);

        public void DrawTracing ()
            => DrawPath (drawPath, Strip.Points, tracingBrush, 1);

        public void DrawReference ()
            => DrawPath (drawReference, Strip.Reference, referenceBrush, 1);

        public void DrawPath (Path _Path, List<II.Waveform.Point> _Points, Brush _Brush, double _Thickness)
            => Tracings.DrawPath (_Path, _Points, _Brush, _Thickness,
                drawGeometry, drawContext, drawXOffset, drawYOffset, drawXMultiplier, drawYMultiplier);

        private void MenuZeroTransducer_Click (object sender, RoutedEventArgs e) {
            switch (Lead.Value) {
                case Lead.Values.ABP: App.Patient.TransducerZeroed_ABP = true; return;
                case Lead.Values.CVP: App.Patient.TransducerZeroed_CVP = true; return;
                case Lead.Values.PA: App.Patient.TransducerZeroed_PA = true; return;
                case Lead.Values.ICP: App.Patient.TransducerZeroed_ICP = true; return;
                case Lead.Values.IAP: App.Patient.TransducerZeroed_IAP = true; return;
            }
        }

        private void MenuAddTracing_Click (object sender, RoutedEventArgs e)
            => App.Device_Monitor.AddTracing ();

        private void MenuRemoveTracing_Click (object sender, RoutedEventArgs e)
            => App.Device_Monitor.RemoveTracing (this);

        private void MenuIncreaseAmplitude_Click (object sender, RoutedEventArgs e) {
            Strip.IncreaseAmplitude ();
            CalculateOffsets ();
        }

        private void MenuDecreaseAmplitude_Click (object sender, RoutedEventArgs e) {
            Strip.DecreaseAmplitude ();
            CalculateOffsets ();
        }

        private void MenuToggleAutoScale_Click (object sender, RoutedEventArgs e) {
            Strip.ScaleAuto = !Strip.ScaleAuto;
            UpdateInterface (null, null);
        }

        private void MenuSelectInputSource (object sender, RoutedEventArgs e) {
            Lead.Values selectedValue;
            if (!Enum.TryParse<Lead.Values> (((MenuItem)sender).Name, out selectedValue))
                return;

            Strip.SetLead (selectedValue);
            Strip.Reset ();
            Strip.Add_Beat__Cardiac_Baseline (App.Patient);
            Strip.Add_Breath__Respiratory_Baseline (App.Patient);

            CalculateOffsets ();
            UpdateInterface (null, null);
        }

        private void canvasTracing_SizeChanged (object sender, SizeChangedEventArgs e) {
            CalculateOffsets ();

            //DrawReference ();
        }
    }
}