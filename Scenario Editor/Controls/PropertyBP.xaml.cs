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

namespace II.Scenario_Editor.Controls {

    public partial class PropertyBP : UserControl {
        public Keys Key;

        public enum Keys {
            NSBP, NDBP, NMAP,               // Non-invasive blood pressures
            ASBP, ADBP, AMAP,               // Arterial line blood pressures
            PSP, PDP, PMP,                  // Pulmonary artery pressures
        }

        public event EventHandler<PropertyIntEventArgs> PropertyChanged;

        public class PropertyIntEventArgs : EventArgs {
            public Keys Key;
            public int Value;
        }

        public PropertyBP (int row, Keys key,
            int systolic, int diastolic,
            int sysInc, int sysMin, int sysMax,
            int diasInc, int diasMin, int diasMax) {
            InitializeComponent ();

            this.SetValue (Grid.RowProperty, row);

            Key = key;
            switch (Key) {
                default: break;
                case Keys.NSBP: lblKey.Content = "Non-invasive Blood Pressure: "; break;
                case Keys.ASBP: lblKey.Content = "Arterial Blood Pressure: "; break;
                case Keys.PSP: lblKey.Content = "Pulmonary Arterial Pressures: "; break;
            }

            numSystolic.Value = systolic;
            numSystolic.Increment = sysInc;
            numSystolic.Minimum = sysMin;
            numSystolic.Maximum = sysMax;
            numSystolic.ValueChanged += sendPropertyChange;
            numSystolic.LostFocus += sendPropertyChange;

            numDiastolic.Value = diastolic;
            numDiastolic.Increment = diasInc;
            numDiastolic.Minimum = diasMin;
            numDiastolic.Maximum = diasMax;
            numDiastolic.ValueChanged += sendPropertyChange;
            numDiastolic.LostFocus += sendPropertyChange;
        }

        private void sendPropertyChange (object sender, EventArgs e) {
            PropertyIntEventArgs ea = new PropertyIntEventArgs ();
            List<Keys> keys = new List<Keys> ();

            switch (Key) {
                default: break;
                case PropertyBP.Keys.NSBP:
                    keys = new List<Keys> () { PropertyBP.Keys.NSBP, PropertyBP.Keys.NDBP, PropertyBP.Keys.NMAP };
                    break;

                case PropertyBP.Keys.ASBP:
                    keys = new List<Keys> () { PropertyBP.Keys.ASBP, PropertyBP.Keys.ADBP, PropertyBP.Keys.AMAP };
                    break;

                case PropertyBP.Keys.PSP:
                    keys = new List<Keys> () { PropertyBP.Keys.PSP, PropertyBP.Keys.PDP, PropertyBP.Keys.PMP };
                    break;
            }

            for (int i = 0; i < keys.Count; i++) {
                ea = new PropertyIntEventArgs ();

                ea.Key = keys [i];

                switch (i) {
                    default: break;
                    case 0: ea.Value = numSystolic.Value ?? 0; break;
                    case 1: ea.Value = numDiastolic.Value ?? 0; break;
                    case 2: ea.Value = Patient.CalculateMAP (numSystolic.Value ?? 0, numDiastolic.Value ?? 0); break;
                }

                PropertyChanged (this, ea);
            }
        }
    }
}