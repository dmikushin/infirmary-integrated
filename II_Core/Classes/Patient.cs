﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace II {
    public class Patient {

        /* Parameters for patient simulation, e.g. vital signs */
        // Vital Signs
        public int  HR, RR, ETCO2, SPO2, CVP,
                    NSBP, NDBP, NMAP,
                    ASBP, ADBP, AMAP,
                    PSP, PDP, PMP;
        public double T;

        // Cardiac Profile
        public double[] STElevation, TElevation;
        public CardiacRhythms CardiacRhythm = new CardiacRhythms();
        public CardiacAxes CardiacAxis = new CardiacAxes ();
        public bool CardiacRhythm_Flag;               // Used for signaling aberrant beats as needed

        // Respiratory Profile
        public RespiratoryRhythms Respiratory_Rhythm = new RespiratoryRhythms();
        public bool Respiratory_Inflated;
        public int Respiratory_IERatio_I, Respiratory_IERatio_E;

        // IABP Settings (stored here for timing and propogation purposes)
        public int IABPRatio = 1;
        public double IABPAugmentation = 0.5;
        public IABPTriggers IABPTrigger = new IABPTriggers ();
        public IABPModes IABPMode = new IABPModes ();
        public bool IABPRunning = false, IABPFilling = false;

        // Obstetric Profile
        public Intensity UCIntensity = new Intensity(),
                         FHRVariability = new Intensity();
        public int UCFrequency, UCDuration, FHR;
        public FetalHeartDecelerations FHRDecelerations = new FetalHeartDecelerations ();


        /* Scales, ratings, etc. for patient parameters */
        public class Intensity {
            public Values Value;
            public enum Values { Absent, Mild, Moderate, Severe }

            public Intensity (Values v) { Value = v; }
            public Intensity () { Value = Values.Absent; }

            public static string LookupString (Values v) {
                return String.Format ("INTENSITY:{0}", Enum.GetValues (typeof (Values)).GetValue ((int)v).ToString ());
            }
        }

        public class IABPTriggers {
            public Values Value;
            public enum Values { ECG, Pressure }

            public IABPTriggers (Values v) { Value = v; }
            public IABPTriggers () { Value = Values.ECG; }

            public static string LookupString (Values v) {
                return String.Format ("IABPTRIGGER:{0}", Enum.GetValues (typeof (Values)).GetValue ((int)v).ToString ());
            }
        }

        public class IABPModes {
            public Values Value;
            public enum Values { Auto, SemiAuto, Manual }

            public IABPModes (Values v) { Value = v; }
            public IABPModes () { Value = Values.Auto; }

            public static string LookupString (Values v) {
                return String.Format ("IABPMODE:{0}", Enum.GetValues (typeof (Values)).GetValue ((int)v).ToString ());
            }
        }


        /* Properties, Counters, Handlers, Timers, etc ... Programmatic Stuff */
        public double HR_Seconds { get { return 60d / Math.Max (1, HR); } }
        public double RR_Seconds { get { return 60d / Math.Max (1, RR); } }
        public double RR_Seconds_I { get { return (RR_Seconds / (Respiratory_IERatio_I + Respiratory_IERatio_E)) * Respiratory_IERatio_I; } }
        public double RR_Seconds_E { get { return (RR_Seconds / (Respiratory_IERatio_I + Respiratory_IERatio_E)) * Respiratory_IERatio_E; } }

        private Timer   timerCardiac_Baseline = new Timer (),
                        timerCardiac_Atrial = new Timer (),
                        timerCardiac_Ventricular = new Timer (),
                        timerRespiratory_Baseline = new Timer (),
                        timerRespiratory_Inspiration = new Timer(),
                        timerRespiratory_Expiration = new Timer(),
                        timerObstetric_Baseline = new Timer(),
                        timerObstetric_ContractionFrequency = new Timer(),
                        timerObstetric_ContractionDuration = new Timer(),
                        timerObstetric_FHRVariationFrequency = new Timer();

        private int counterCardiac;

        public static int CalculateMAP (int sbp, int dbp) {
            return dbp + ((sbp - dbp) / 3);
        }

        public event EventHandler<PatientEvent_Args> PatientEvent;
        public class PatientEvent_Args : EventArgs {
            public Patient Patient;
            public EventTypes EventType;

            public PatientEvent_Args (Patient p, EventTypes e) {
                Patient = p;
                EventType = e;
            }

            public enum EventTypes {
                Vitals_Change,
                Cardiac_Baseline,
                Cardiac_Atrial,
                Cardiac_Ventricular,
                Respiratory_Baseline,
                Respiratory_Inspiration,
                Respiratory_Expiration,
                IABP_Inflate,
                IABP_Deflate,
                Obstetric_Baseline,
                Obstetric_ContractionStart,
                Obstetric_ContractionEnd,
                Obstetric_FetalHeartVariation
            }
        }


        public Patient () {
            UpdateParameters (80, 18, 98,
                            38.0f, 6, 40,
                            120, 80, 95,
                            120, 80, 95,
                            22, 12, 16,
                            new double[] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f },
                            new double[] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f },
                            CardiacRhythms.Values.Sinus_Rhythm,
                            RespiratoryRhythms.Values.Regular,
                            1, 1,
                            150, Intensity.Values.Absent, new List<FetalHeartDecelerations.Values>(),
                            60, 30, Intensity.Values.Moderate);

            InitTimers ();
            SetTimers ();
        }

        public void Timers_Process (object sender, EventArgs e) {
            /* For cross-platform compatibility with different timers ...
             * When creating a Patient object, create a native thread-safe Timer object,
             * short interval, and call this function on its Tick to process all Patient
             * timers.
             */
            timerCardiac_Baseline.Process ();
            timerCardiac_Atrial.Process ();
            timerCardiac_Ventricular.Process ();
            timerRespiratory_Baseline.Process ();
            timerRespiratory_Inspiration.Process ();
            timerRespiratory_Expiration.Process ();
            timerObstetric_Baseline.Process ();
            timerObstetric_ContractionDuration.Process ();
            timerObstetric_ContractionFrequency.Process ();
            timerObstetric_FHRVariationFrequency.Process ();
        }

        public void Load_Process (string inc) {
            StringReader sRead = new StringReader (inc);

            try {
                string line;
                while ((line = sRead.ReadLine()) != null) {
                    if (line.Contains(":")) {
                        string pName = line.Substring (0, line.IndexOf (':')),
                                pValue = line.Substring (line.IndexOf (':') + 1);
                        switch (pName) {
                            default: break;
                            case "HR": HR = int.Parse (pValue); break;
                            case "RR": RR = int.Parse (pValue); break;
                            case "ETCO2": ETCO2 = int.Parse (pValue); break;
                            case "SPO2": SPO2 = int.Parse (pValue); break;
                            case "CVP": CVP = int.Parse (pValue); break;
                            case "NSBP": NSBP = int.Parse (pValue); break;
                            case "NDBP": NDBP = int.Parse (pValue); break;
                            case "NMAP": NMAP = int.Parse (pValue); break;
                            case "ASBP": ASBP = int.Parse (pValue); break;
                            case "ADBP": ADBP = int.Parse (pValue); break;
                            case "AMAP": AMAP = int.Parse (pValue); break;
                            case "PSP": PSP = int.Parse (pValue); break;
                            case "PDP": PDP = int.Parse (pValue); break;
                            case "PMP": PMP = int.Parse (pValue); break;
                            case "T": T = int.Parse (pValue); break;
                            case "ST_Elevation":
                                string[] e_st = pValue.Split (',');
                                for (int i = 0; i < e_st.Length; i++)
                                    STElevation [i] = double.Parse (e_st [i]);
                                break;
                            case "T_Elevation":
                                string [] e_t = pValue.Split (',');
                                for (int i = 0; i < e_t.Length; i++)
                                    TElevation [i] = double.Parse (e_t [i]);
                                break;
                            case "Cardiac_Rhythm": CardiacRhythm.Value = (CardiacRhythms.Values) Enum.Parse(typeof(CardiacRhythms.Values), pValue); break;
                            case "Cardiac_Rhythm__Flag": CardiacRhythm_Flag = bool.Parse (pValue); break;
                            case "Cardiac_Axis_Shift": CardiacAxis.Value = (CardiacAxes.Values)Enum.Parse (typeof (CardiacAxes.Values), pValue); break;

                            case "IABPRatio": IABPRatio = int.Parse (pValue); break;
                            case "IABPAugmentation": IABPAugmentation = double.Parse (pValue); break;
                            case "IABPTrigger": IABPTrigger.Value = (IABPTriggers.Values)Enum.Parse (typeof (IABPTriggers.Values), pValue); break;
                            case "IABPMode": IABPMode.Value = (IABPModes.Values)Enum.Parse (typeof (IABPModes.Values), pValue); break;
                            case "IABPRunning": IABPRunning = bool.Parse (pValue); break;
                            case "IABPFilling": IABPFilling = bool.Parse (pValue); break;

                            case "Respiratory_Rhythm": Respiratory_Rhythm.Value = (RespiratoryRhythms.Values)Enum.Parse (typeof (RespiratoryRhythms.Values), pValue); break;
                            case "Respiratory_Inflated": Respiratory_Inflated = bool.Parse (pValue); break;
                            case "Respiratory_IERatio_I": Respiratory_IERatio_I = int.Parse (pValue); break;
                            case "Respiratory_IERatio_E": Respiratory_IERatio_E = int.Parse (pValue); break;

                            case "FHR": FHR = int.Parse (pValue); break;
                            case "FHR_Variability": FHRVariability.Value = (Intensity.Values)Enum.Parse (typeof (Intensity.Values), pValue); break;
                            case "FHR_Rhythms":
                                foreach (string fhr_rhythm in pValue.Split (','))
                                    FHRDecelerations.ValueList.Add ((FetalHeartDecelerations.Values)Enum.Parse (typeof (FetalHeartDecelerations.Values), fhr_rhythm));
                                break;
                            case "UterineContraction_Frequency": UCFrequency = int.Parse (pValue); break;
                            case "UterineContraction_Duration": UCDuration = int.Parse (pValue); break;
                            case "UterineContraction_Intensity": UCIntensity.Value = (Intensity.Values)Enum.Parse (typeof (Intensity.Values), pValue); break;
                        }
                    }
                }
            } catch {
                sRead.Close ();
                return;
            }

            sRead.Close ();

            SetTimers ();
            OnCardiac_Baseline ();
            OnRespiratory_Baseline ();

            PatientEvent?.Invoke (this, new PatientEvent_Args (this, PatientEvent_Args.EventTypes.Vitals_Change));
        }

        public string Save () {
            StringBuilder sWrite = new StringBuilder ();

            sWrite.AppendLine (String.Format ("{0}:{1}", "HR", HR));
            sWrite.AppendLine (String.Format ("{0}:{1}", "RR", RR));
            sWrite.AppendLine (String.Format ("{0}:{1}", "ETCO2", ETCO2));
            sWrite.AppendLine (String.Format ("{0}:{1}", "SPO2", SPO2));
            sWrite.AppendLine (String.Format ("{0}:{1}", "CVP", CVP));
            sWrite.AppendLine (String.Format ("{0}:{1}", "NSBP", NSBP));
            sWrite.AppendLine (String.Format ("{0}:{1}", "NDBP", NDBP));
            sWrite.AppendLine (String.Format ("{0}:{1}", "NMAP", NMAP));
            sWrite.AppendLine (String.Format ("{0}:{1}", "ASBP", ASBP));
            sWrite.AppendLine (String.Format ("{0}:{1}", "ADBP", ADBP));
            sWrite.AppendLine (String.Format ("{0}:{1}", "AMAP", AMAP));
            sWrite.AppendLine (String.Format ("{0}:{1}", "PSP", PSP));
            sWrite.AppendLine (String.Format ("{0}:{1}", "PDP", PDP));
            sWrite.AppendLine (String.Format ("{0}:{1}", "PMP", PMP));
            sWrite.AppendLine (String.Format ("{0}:{1}", "T", T));
            sWrite.AppendLine (String.Format ("{0}:{1}", "ST_Elevation", string.Join(",", STElevation)));
            sWrite.AppendLine (String.Format ("{0}:{1}", "T_Elevation", string.Join(",", TElevation)));
            sWrite.AppendLine (String.Format ("{0}:{1}", "Cardiac_Rhythm", CardiacRhythm.Value));
            sWrite.AppendLine (String.Format ("{0}:{1}", "Cardiac_Rhythm__Flag", CardiacRhythm_Flag));
            sWrite.AppendLine (String.Format ("{0}:{1}", "Cardiac_Axis_Shift", CardiacAxis.Value));

            sWrite.AppendLine (String.Format ("{0}:{1}", "IABPRatio", IABPRatio));
            sWrite.AppendLine (String.Format ("{0}:{1}", "IABPAugmentation", IABPAugmentation));
            sWrite.AppendLine (String.Format ("{0}:{1}", "IABPTrigger", IABPTrigger.Value));
            sWrite.AppendLine (String.Format ("{0}:{1}", "IABPMode", IABPMode.Value));
            sWrite.AppendLine (String.Format ("{0}:{1}", "IABPRunning", IABPRunning));
            sWrite.AppendLine (String.Format ("{0}:{1}", "IABPFilling", IABPFilling));

            sWrite.AppendLine (String.Format ("{0}:{1}", "Respiratory_Rhythm", Respiratory_Rhythm.Value));
            sWrite.AppendLine (String.Format ("{0}:{1}", "Respiratory_Inflated", Respiratory_Inflated));
            sWrite.AppendLine (String.Format ("{0}:{1}", "Respiratory_IERatio_I", Respiratory_IERatio_I));
            sWrite.AppendLine (String.Format ("{0}:{1}", "Respiratory_IERatio_E", Respiratory_IERatio_E));

            sWrite.AppendLine (String.Format ("{0}:{1}", "FHR", FHR));
            sWrite.AppendLine (String.Format ("{0}:{1}", "FHR_Variability", FHRVariability.Value));
            sWrite.AppendLine (String.Format ("{0}:{1}", "UterineContraction_Frequency", UCFrequency));
            sWrite.AppendLine (String.Format ("{0}:{1}", "UterineContraction_Duration", UCDuration));
            sWrite.AppendLine (String.Format ("{0}:{1}", "UterineContraction_Intensity", UCIntensity.Value));
            sWrite.AppendLine (String.Format ("{0}:{1}", "FHR_Rhythms", string.Join (",", FHRDecelerations.ValueList)));

            return sWrite.ToString ();
        }

        public void UpdateParameters(
                    int hr,     int rr,     int spo2,
                    double t,
                    int cvp,    int etco2,
                    int nsbp,   int ndbp,   int nmap,
                    int asbp,   int adbp,   int amap,
                    int psp,    int pdp,    int pmp,
                    double[] st_elev,        double[] t_elev,
                    CardiacRhythms.Values      card_rhythm,
                    RespiratoryRhythms.Values  resp_rhythm,
                    int resp_ier_i, int resp_ier_e,
                    int fhr, Intensity.Values fhr_var, List<FetalHeartDecelerations.Values> fhr_rhythms,
                    int uc_freq, int uc_duration, Intensity.Values uc_intensity ) {

            HR = hr;    RR = rr;    SPO2 = spo2;
            T = t;
            CVP = cvp;  ETCO2 = etco2;

            NSBP = nsbp;    NDBP = ndbp;    NMAP = nmap;
            ASBP = asbp;    ADBP = adbp;    AMAP = amap;
            PSP = psp;      PDP = pdp;      PMP = pmp;

            CardiacRhythm.Value = card_rhythm;
            STElevation = st_elev;
            TElevation = t_elev;

            Respiratory_Rhythm.Value = resp_rhythm;
            Respiratory_IERatio_I = resp_ier_i;
            Respiratory_IERatio_E = resp_ier_e;

            FHR = fhr;
            FHRVariability.Value = fhr_var;
            FHRDecelerations.ValueList = fhr_rhythms;
            UCFrequency = uc_freq;
            UCDuration = uc_duration;
            UCIntensity.Value = uc_intensity;

            SetTimers ();
            OnCardiac_Baseline ();
            OnRespiratory_Baseline ();
            PatientEvent?.Invoke (this, new PatientEvent_Args (this, PatientEvent_Args.EventTypes.Vitals_Change));
        }

        public void ClampVitals (
                    int hrMin, int hrMax,
                    int spo2Min, int spo2Max,
                    int etco2Min, int etco2Max,
                    int sbpMin, int sbpMax, int dbpMin, int dbpMax,
                    int pspMin, int pspMax, int pdpMin, int pdpMax) {

            HR = Utility.Clamp (HR, hrMin, hrMax);
            SPO2 = Utility.Clamp (SPO2, spo2Min, spo2Max);
            ETCO2 = Utility.Clamp (ETCO2, etco2Min, etco2Max);

            NSBP = Utility.Clamp (NSBP, sbpMin, sbpMax);
            NDBP = Utility.Clamp (NDBP, dbpMin, dbpMax);
            NMAP = Patient.CalculateMAP (NSBP, NDBP);

            ASBP = Utility.Clamp (ASBP, sbpMin, sbpMax);
            ADBP = Utility.Clamp (ADBP, sbpMin, sbpMax);
            AMAP = Patient.CalculateMAP (ASBP, ADBP);

            PSP = Utility.Clamp (PSP, pspMin, pspMax);
            PDP = Utility.Clamp (PDP, pdpMin, pdpMax);
            PMP = Patient.CalculateMAP (PSP, PDP);

            SetTimers ();
            OnCardiac_Baseline ();
            OnRespiratory_Baseline ();
            PatientEvent?.Invoke (this, new PatientEvent_Args (this, PatientEvent_Args.EventTypes.Vitals_Change));
        }

        private void InitTimers() {
            timerCardiac_Baseline.Tick += delegate { OnCardiac_Baseline (); };
            timerCardiac_Atrial.Tick += delegate { OnCardiac_Atrial (); };
            timerCardiac_Ventricular.Tick += delegate { OnCardiac_Ventricular (); };

            timerRespiratory_Baseline.Tick += delegate { OnRespiratory_Baseline (); };
            timerRespiratory_Inspiration.Tick += delegate { OnRespiratory_Inspiration (); };
            timerRespiratory_Expiration.Tick += delegate { OnRespiratory_Expiration (); };

            timerObstetric_Baseline.Tick += delegate { OnObstetric_Baseline (); };
            timerObstetric_ContractionFrequency.Tick += delegate { OnObstetric_ContractionStart (); };
            timerObstetric_ContractionDuration.Tick += delegate { OnObstetric_ContractionEnd (); };
            timerObstetric_FHRVariationFrequency.Tick += delegate { OnObstetric_FetalHeartVariationStart (); };
        }

        private void SetTimers() {
            timerCardiac_Baseline.Reset((int) (HR_Seconds * 1000f));
            timerCardiac_Atrial.Stop ();
            timerCardiac_Ventricular.Stop ();

            timerRespiratory_Baseline.Reset ((int)(RR_Seconds * 1000f));
            timerRespiratory_Inspiration.Stop ();
            timerRespiratory_Expiration.Stop ();

            timerObstetric_Baseline.Reset(1000);
            timerObstetric_ContractionDuration.Stop ();
            timerObstetric_ContractionFrequency.Stop ();
            timerObstetric_FHRVariationFrequency.Stop ();
        }

        private void OnCardiac_Baseline() {
            PatientEvent?.Invoke (this, new PatientEvent_Args (this, PatientEvent_Args.EventTypes.Cardiac_Baseline));
            timerCardiac_Baseline.Set ((int)(HR_Seconds * 1000f));

            switch (CardiacRhythm.Value) {
                default:
                case CardiacRhythms.Values.Asystole:
                    break;

                // Traced as "regular V" Rhythms
                case CardiacRhythms.Values.Atrial_Flutter:
                case CardiacRhythms.Values.Junctional:
                case CardiacRhythms.Values.Idioventricular:
                case CardiacRhythms.Values.Supraventricular_Tachycardia:
                case CardiacRhythms.Values.Ventricular_Tachycardia_Monomorphic_Pulsed:
                case CardiacRhythms.Values.Ventricular_Tachycardia_Monomorphic_Pulseless:
                case CardiacRhythms.Values.Ventricular_Tachycardia_Polymorphic:
                case CardiacRhythms.Values.Ventricular_Fibrillation_Coarse:
                case CardiacRhythms.Values.Ventricular_Fibrillation_Fine:
                    timerCardiac_Ventricular.Reset (1);
                    break;

                // Traced as "regular A" or "regular A -> V" Rhythms
                case CardiacRhythms.Values.AV_Block__1st_Degree:
                case CardiacRhythms.Values.AV_Block__Mobitz_II:
                case CardiacRhythms.Values.AV_Block__Wenckebach:
                case CardiacRhythms.Values.Bundle_Branch_Block:
                case CardiacRhythms.Values.Sinus_Rhythm:
                case CardiacRhythms.Values.Pulseless_Electrical_Activity:
                case CardiacRhythms.Values.Ventricular_Standstill:
                    timerCardiac_Atrial.Reset (1);
                    break;

                // Traced as "irregular V" rhythms
                case CardiacRhythms.Values.Atrial_Fibrillation:
                    timerCardiac_Baseline.Set ((int)(timerCardiac_Baseline.Interval * Utility.RandomDouble (0.6, 1.4)));
                    timerCardiac_Ventricular.Reset (1);
                    break;

                /* Special Cases */
                case CardiacRhythms.Values.AV_Block__3rd_Degree:
                    timerCardiac_Atrial.Reset ((int)(timerCardiac_Baseline.Interval * 0.6));
                    timerCardiac_Ventricular.Reset (160);
                    break;

                case CardiacRhythms.Values.Sinus_Rhythm_with_PACs:
                    counterCardiac -= 1;
                    if (counterCardiac <= 0) {
                        counterCardiac = new Random ().Next (4, 8);
                        timerCardiac_Baseline.Set ((int)(timerCardiac_Baseline.Interval * Utility.RandomDouble (0.6, 0.8)));
                    }
                    timerCardiac_Atrial.Reset (1);
                    break;

                case CardiacRhythms.Values.Sinus_Rhythm_with_PJCs:
                    counterCardiac -= 1;
                    if (counterCardiac <= 0) {
                        counterCardiac = new Random ().Next (4, 8);
                        timerCardiac_Ventricular.Reset (1);
                    } else {
                        if (counterCardiac == 1)
                            timerCardiac_Baseline.Set ((int)(timerCardiac_Baseline.Interval * Utility.RandomDouble (0.7, 0.9)));
                        timerCardiac_Atrial.Reset (1);
                    }
                    break;

                case CardiacRhythms.Values.Sinus_Rhythm_with_Bigeminy:
                case CardiacRhythms.Values.Sinus_Rhythm_with_Trigeminy:
                    counterCardiac -= 1;
                    if (counterCardiac == 0) {
                        timerCardiac_Baseline.Set ((int)(timerCardiac_Baseline.Interval * 0.8));
                    } else if (counterCardiac < 0) {   // Then throw the PVC and reset the counters
                        if (CardiacRhythm.Value == CardiacRhythms.Values.Sinus_Rhythm_with_Bigeminy)
                            counterCardiac = 1;
                        else if (CardiacRhythm.Value == CardiacRhythms.Values.Sinus_Rhythm_with_Trigeminy)
                            counterCardiac = 2;
                        CardiacRhythm_Flag = true;
                        timerCardiac_Ventricular.Reset (1);
                        break;
                    }
                    timerCardiac_Atrial.Reset (1);
                    break;

                case CardiacRhythms.Values.Sinus_Rhythm_with_PVCs_Unifocal:
                case CardiacRhythms.Values.Sinus_Rhythm_with_PVCs_Multifocal:
                    counterCardiac -= 1;
                    if (counterCardiac == 0 || CardiacRhythm_Flag) {  // Shorten the beat preceding the PVC, making it premature
                        timerCardiac_Baseline.Set ((int)(timerCardiac_Baseline.Interval * 0.8));
                    }
                    if (counterCardiac < 0 || CardiacRhythm_Flag) {   // Then throw the PVC and reset the counters
                        counterCardiac = new Random().Next(4, 9);
                        CardiacRhythm_Flag = true;
                        timerCardiac_Ventricular.Reset (1);
                        break;
                    }
                    timerCardiac_Atrial.Reset (1);
                    break;
            }
        }

        private void OnCardiac_Atrial () {

            PatientEvent?.Invoke (this, new PatientEvent_Args (this, PatientEvent_Args.EventTypes.Cardiac_Atrial));

            switch (CardiacRhythm.Value) {
                default:
                case CardiacRhythms.Values.Asystole:
                    break;

                // Regular A Rhythms
                case CardiacRhythms.Values.Ventricular_Standstill:
                    timerCardiac_Atrial.Stop ();
                    break;

                // Regular A -> V rhythms
                case CardiacRhythms.Values.Bundle_Branch_Block:
                case CardiacRhythms.Values.Sinus_Rhythm:
                case CardiacRhythms.Values.Sinus_Rhythm_with_PACs:
                case CardiacRhythms.Values.Sinus_Rhythm_with_PJCs:
                case CardiacRhythms.Values.Sinus_Rhythm_with_Bigeminy:
                case CardiacRhythms.Values.Sinus_Rhythm_with_Trigeminy:
                case CardiacRhythms.Values.Sinus_Rhythm_with_PVCs_Unifocal:
                case CardiacRhythms.Values.Sinus_Rhythm_with_PVCs_Multifocal:
                case CardiacRhythms.Values.Pulseless_Electrical_Activity:
                    timerCardiac_Atrial.Stop ();
                    timerCardiac_Ventricular.Reset (160);
                    break;

                /* Special cases */

                case CardiacRhythms.Values.AV_Block__1st_Degree:
                    timerCardiac_Atrial.Stop ();
                    timerCardiac_Ventricular.Reset (240);
                    break;

                case CardiacRhythms.Values.AV_Block__Mobitz_II:
                    timerCardiac_Atrial.Stop ();
                    counterCardiac += 1;
                    if (counterCardiac > 2) {
                        counterCardiac = 0;
                    } else {
                        timerCardiac_Ventricular.Reset (160);
                    }
                    break;

                case CardiacRhythms.Values.AV_Block__Wenckebach:
                    timerCardiac_Atrial.Stop ();
                    counterCardiac += 1;
                    if (counterCardiac >= 4) {
                        counterCardiac = 0;
                    } else {
                        timerCardiac_Baseline.Set ((int)(timerCardiac_Baseline.Interval + (160 * counterCardiac)));
                        timerCardiac_Ventricular.Reset ((int)(160 * counterCardiac));
                    }
                    break;

                case CardiacRhythms.Values.AV_Block__3rd_Degree:
                    // Specifically let atrial timer continue to run and propogate P-waves!
                    break;
            }
        }

        private void OnCardiac_Ventricular () {
            PatientEvent?.Invoke (this, new PatientEvent_Args (this, PatientEvent_Args.EventTypes.Cardiac_Ventricular));

            switch (CardiacRhythm.Value) {
                default:
                    CardiacRhythm_Flag = false;
                    break;

                case CardiacRhythms.Values.Sinus_Rhythm_with_PVCs_Unifocal:
                    CardiacRhythm_Flag = new Random ().Next (0, 7) == 0;       // 1/7 chance to potentiate runs of PVCs
                    break;
            }

            timerCardiac_Ventricular.Stop ();
        }

        private void OnRespiratory_Baseline() {
            PatientEvent?.Invoke (this, new PatientEvent_Args (this, PatientEvent_Args.EventTypes.Respiratory_Baseline));
            timerRespiratory_Baseline.Set ((int)(RR_Seconds * 1000f));

            switch (Respiratory_Rhythm.Value) {
                default:
                case RespiratoryRhythms.Values.Apnea:
                    break;

                case RespiratoryRhythms.Values.Regular:
                    timerRespiratory_Inspiration.Reset (1);
                    break;
            }
        }

        private void OnRespiratory_Inspiration() {
            Respiratory_Inflated = true;
            PatientEvent?.Invoke (this, new PatientEvent_Args (this, PatientEvent_Args.EventTypes.Respiratory_Inspiration));
            timerRespiratory_Inspiration.Stop ();

            switch (Respiratory_Rhythm.Value) {
                default:
                case RespiratoryRhythms.Values.Apnea:
                    break;

                case RespiratoryRhythms.Values.Regular:
                    timerRespiratory_Expiration.Reset ((int)(RR_Seconds_I * 1000f));     // Expiration.Interval marks end inspiration
                    break;
            }
        }

        private void OnRespiratory_Expiration() {
            Respiratory_Inflated = false;
            PatientEvent?.Invoke (this, new PatientEvent_Args (this, PatientEvent_Args.EventTypes.Respiratory_Expiration));
            timerRespiratory_Expiration.Stop ();
        }

        private void OnObstetric_Baseline () {
            PatientEvent?.Invoke (this, new PatientEvent_Args (this, PatientEvent_Args.EventTypes.Obstetric_Baseline));

            if (UCFrequency > 0 && !timerObstetric_ContractionDuration.IsRunning) {
                timerObstetric_ContractionFrequency.Continue(UCFrequency * 1000);
            } else if (UCFrequency <= 0) {
                timerObstetric_ContractionDuration.Stop ();
                timerObstetric_ContractionFrequency.Stop ();
            }

            if (FHRVariability.Value == Intensity.Values.Absent)
                timerObstetric_FHRVariationFrequency.Stop ();
            else
                timerObstetric_FHRVariationFrequency.Continue(20000);
        }

        private void OnObstetric_ContractionStart () {
            PatientEvent?.Invoke (this, new PatientEvent_Args (this, PatientEvent_Args.EventTypes.Obstetric_ContractionStart));
            timerObstetric_ContractionDuration.Reset(UCDuration * 1000);
        }

        private void OnObstetric_ContractionEnd () {
            PatientEvent?.Invoke (this, new PatientEvent_Args (this, PatientEvent_Args.EventTypes.Obstetric_ContractionEnd));
            timerObstetric_ContractionDuration.Stop ();
        }

        private void OnObstetric_FetalHeartVariationStart () {
            PatientEvent?.Invoke (this, new PatientEvent_Args (this, PatientEvent_Args.EventTypes.Obstetric_FetalHeartVariation));
        }
    }
}
