using Hast.Layer;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hast.Samples.Kpz
{
    ///<summary>The main form showing the log and the output graph of the algorithm.</summary>
    public partial class ChartForm : Form
    {
        private int _numKpzIterations { get { return (int)nudIterations.Value; } }
        private int _kpzWidth { get { return (int)nudTableWidth.Value; } }
        private int _kpzHeight { get { return (int)nudTableHeight.Value; } }
        private bool _showInspector { get { return checkShowInspector.Checked; } }
        private bool _writeToFile { get { return checkWriteToFile.Checked; } }
        private bool _verifyOutput { get { return checkVerifyOutput.Checked; } }
        private bool _stepByStep { get { return checkStep.Checked; } }
        private bool _randomSeedEnable { get { return checkRandomSeed.Checked; } }

        /// <summary>
        /// The BackgroundWorker is used to run the algorithm on a different CPU thread than the GUI,
        /// so that the GUI keeps responding while the algorithm is running.
        /// </summary>
        BackgroundWorker _backgroundWorker;

        /// <summary>
        /// The Kpz object is used to perform the KPZ algorithm, to input its parameters and return the result.
        /// </summary>
        Kpz _kpz;

        /// <summary>InspectForm allows us to inspect the results of the KPZ algorithm on a GUI interface.</summary>
        InspectForm _inspectForm;

        /// <summary>
        /// The constructor initializes the <see cref="_backgroundWorker" />.
        /// </summary>
        public ChartForm()
        {
            InitializeComponent();
            _backgroundWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            _backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            _backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
                backgroundWorker_RunWorkerCompleted
            );
            comboTarget.SelectedIndex = 3;
        }

        /// <summary>It adds a line to the log.</summary>
        private void LogIt(string what)
        {
            listLog.Items.Add(what);
            listLog.SelectedIndex = listLog.Items.Count - 1;
        }

        /// <summary>
        /// Clicking on <see cref="buttonStart" /> starts the KPZ algorithm in the background.
        /// When the algorithm is running, it can also be used to stop it.
        /// </summary>
        private void buttonStart_Click(object sender, EventArgs e)
        {
            panelTop.Enabled = false;
            if (!_backgroundWorker.IsBusy)
            {
                buttonStart.Text = "Stop";
                progressBar.Maximum = _numKpzIterations;
                ComputationTarget = CurrentComputationTarget; //ComboBox value cannot be accessed from BackgroundWorker
                _backgroundWorker.RunWorkerAsync(2000);
            }
            else
            {
                buttonStart.Text = "Start";
                _backgroundWorker.CancelAsync();
            }
        }

        /// <summary>
        /// It runs the KPZ algorithm in the background.
        /// See also: <see cref="BackgroundWorker"/>
        /// </summary>
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Do not access the form's BackgroundWorker reference directly.
            // Instead, use the reference provided by the sender parameter.
            var bw = sender as BackgroundWorker;
            RunKpz(bw);
            // If the operation was canceled by the user, set the DoWorkEventArgs.Cancel property to true.
            if (bw.CancellationPending) e.Cancel = true;
        }

        /// <summary>
        /// It is called when the <see cref="BackgroundWorker"/> is finished with the KPZ algorithm.
        /// </summary>
        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled) LogIt("Operation was canceled.");
            else if (e.Error != null) LogIt(String.Format("An error occurred: {0}", e.Error.Message));
            else LogIt("Operation finished.");
            buttonStart.Enabled = false;
            progressBar.Visible = false;

            if (_showInspector)
            {
                if (_writeToFile)
                {
                    LogIt("Writing KpzStateLogger to file...");
                    var kpzStateLoggerPath = System.IO.Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\kpzStateLogger\";
                    if (!System.IO.Directory.Exists(kpzStateLoggerPath))
                        System.IO.Directory.CreateDirectory(kpzStateLoggerPath);
                    _kpz.StateLogger.WriteToFiles(kpzStateLoggerPath);
                }

                LogIt("Opening KpzStateLogger window...");
                _inspectForm = new InspectForm(_kpz.StateLogger);
                _inspectForm.Show();
            }
            LogIt("Done.");
        }

        /// <summary>
        /// GUI controls cannot be directly updated from inside the <see cref="BackgroundWorker"/>.
        /// One solution for this is using <see cref="System.Windows.Threading.Dispatcher.Invoke"/> to
        /// schedule an update in the GUI thread.
        /// AsyncLogIt schedules a <see cref="LogIt"/> operation on the GUI thread from within the
        /// <see cref="BackgroundWorker"/>.
        /// </summary>
        private void AsyncLogIt(string what)
        {
            Invoke(new Action(() =>
            {
                LogIt(what);
            }));
        }

        /// <summary>
        /// AsyncUpdateProgressBar schedules the progress bar value to be updated in GUI thread from within the
        /// <see cref="BackgroundWorker"/>. For more info, see <see cref="AsyncLogIt"/>
        /// </summary>
        private void AsyncUpdateProgressBar(int progress)
        {
            Invoke(new Action(() =>
            {
                progressBar.Value = progress;
            }));
        }

        /// <summary>
        /// AsyncUpdateChart schedules the chart to be updated in GUI thread from within the
        /// <see cref="BackgroundWorker"/>. For more info, see <see cref="AsyncLogIt"/>
        /// </summary>
        private void AsyncUpdateChart(int iteration, bool forceUpdate = false)
        {
            // The chart is updated (and the statistics are calculated) 10 times in every logarithmic scale step, e.g.
            // in iterations: 1,2,3,4,5,6,7,8,9, 10,20,30,40,50,60,70,80,90, 100,200,300,400,500...
            int iterationNext10Pow = (int)Math.Pow(10, Math.Floor(Math.Log10(iteration) + 1));
            bool updateChartInThisIteartion = forceUpdate || iteration < 10 ||
                ((iteration + 1) % (iterationNext10Pow / 10)) == 0;

            if (updateChartInThisIteartion)
            {
                double mean;
                bool periodicityValid;
                int periodicityInvalidXCount;
                int periodicityInvalidYCount;
                int[,] heightMap = _kpz.GenerateHeightMap(
                    out mean, out periodicityValid, out periodicityInvalidXCount, out periodicityInvalidYCount);

                if (!periodicityValid)
                {
                    AsyncLogIt(String.Format("Warning: Periodicity invalid (x: {0}, y: {1})",
                        periodicityInvalidXCount, periodicityInvalidYCount));
                }

                double surfaceRoughness = _kpz.HeightMapStandardDeviation(heightMap, mean);

                Invoke(new Action(() =>
                {
                    LogIt(String.Format("iteration: {0}, surfaceRoughness: {1}", iteration, surfaceRoughness));
                    chartKPZ.Series[0].Points.AddXY(iteration + 1, surfaceRoughness);
                    chartKPZ.ChartAreas[0].AxisX.IsLogarithmic = true;
                }));
            }
        }

        /// <summary>
        /// This is the function that is ran in the background by <see cref="BackgroundWorker"/>.
        /// </summary>
        private void RunKpz(BackgroundWorker bw)
        {
            AsyncLogIt("Creating KPZ data structure...");
            _kpz = new Kpz(_kpzWidth, _kpzHeight, 0.5, 0.5, _showInspector, ComputationTarget);
            AsyncLogIt("Filling grid with initial data...");
            _kpz.InitializeGrid();

            IHastlayer hastlayer = null;

            if (ComputationTarget != KpzTarget.Cpu)
            {
                AsyncLogIt("Initializing Hastlayer...");
                _kpz.LogItFunction = AsyncLogIt;
                Task<IHastlayer> hastlayerInitializationTask = _kpz.InitializeHastlayer(_verifyOutput, _randomSeedEnable);
                hastlayerInitializationTask.Wait();
                hastlayer = hastlayerInitializationTask.Result;
            }

            try
            {
                if (ComputationTarget == KpzTarget.PrngTest) return; // Already done test inside InitializeHastlayer

                var sw = System.Diagnostics.Stopwatch.StartNew();
                AsyncLogIt("Starting KPZ iterations...");

                if (!ComputationTarget.HastlayerParallelizedAlgorithm())
                {
                    for (int currentIteration = 0; currentIteration < _numKpzIterations; currentIteration++)
                    {
                        if (ComputationTarget == KpzTarget.Cpu)
                        {
                            _kpz.DoIteration();
                        }
                        else
                        {
                            if (_stepByStep) _kpz.DoHastIterationDebug();
                            else { _kpz.DoHastIterations((uint)_numKpzIterations); break; }
                        }
                        AsyncUpdateProgressBar(currentIteration);
                        AsyncUpdateChart(currentIteration);
                        if (bw.CancellationPending) return;
                    }
                }
                else
                {
                    int currentIteration = 1;
                    int lastIteration = 0;
                    for (; ; )
                    {
                        int iterationsToDo = currentIteration - lastIteration;
                        AsyncLogIt(String.Format("Doing {0} iterations at once...", iterationsToDo));
                        _kpz.DoHastIterations((uint)iterationsToDo);
                        AsyncUpdateProgressBar(currentIteration);
                        // Force update if current iteration is the last:
                        AsyncUpdateChart(currentIteration - 1, currentIteration == _numKpzIterations);
                        if (currentIteration >= _numKpzIterations) break;
                        lastIteration = currentIteration;
                        currentIteration *= 10;
                        if (currentIteration > _numKpzIterations) currentIteration = _numKpzIterations;
                    }
                }
                sw.Stop();
                AsyncLogIt("Done. Total time measured: " + sw.ElapsedMilliseconds + " ms");
            }
            finally
            {
                if (hastlayer != null) hastlayer.Dispose();
            }
        }

        /// <summary>
        /// <see cref="labelShowInspector" /> is next to <see cref="checkShowInspector" />, and clicking on it
        /// can be used to toggle the checkbox.
        /// </summary>
        private void labelShowInspector_Click(object sender, EventArgs e)
        {
            checkShowInspector.Checked = !checkShowInspector.Checked;
        }

        private void comboTarget_SelectedIndexChanged(object sender, EventArgs e)
        {
            nudTableWidth.Enabled = nudTableHeight.Enabled = comboTarget.SelectedIndex == 0;
            checkStep.Enabled = comboTarget.SelectedIndex != 0;
            checkVerifyOutput.Enabled =
                comboTarget.SelectedIndex == 2 || comboTarget.SelectedIndex == 4 || comboTarget.SelectedIndex == 5;
            if (comboTarget.SelectedIndex == 5) checkVerifyOutput.Checked = true;
            if (comboTarget.SelectedIndex > 0 && comboTarget.SelectedIndex <= 2)
            {
                nudTableWidth.Value = nudTableHeight.Value = 8;
                nudIterations.Value = 1;
            }
            else if (comboTarget.SelectedIndex > 2)
            {
                nudTableWidth.Value = nudTableHeight.Value = KpzKernelsParallelizedInterface.GridSize;
                nudIterations.Value = 1;
            }
        }

        /// <summary>This property of the <see cref="ChartForm"/> maps the selected item in <see cref="comboTarget"/> to 
        /// <see cref="KpzTarget"/> values.</summary>
        private KpzTarget CurrentComputationTarget
        {
            get
            {
                switch (comboTarget.SelectedIndex)
                {
                    case 0: return KpzTarget.Cpu;
                    case 1: return KpzTarget.FpgaSimulation;
                    case 2: return KpzTarget.Fpga;
                    case 3: return KpzTarget.FpgaSimulationParallelized;
                    case 4: return KpzTarget.FpgaParallelized;
                    case 5: return KpzTarget.PrngTest;
                }
                return KpzTarget.Cpu;
            }
        }

        KpzTarget ComputationTarget;

        /// <summary>
        /// The label next to the checkbox should also trigger its state. (The checkbox cannot have a label at the left,
        /// that's why we need to use an external label for it.)
        /// </summary>
        private void labelVerifyOutput_Click(object sender, EventArgs e)
        {
            if (checkVerifyOutput.Enabled) checkVerifyOutput.Checked = !checkVerifyOutput.Checked;
        }

        /// <summary>
        /// The label next to the checkbox should also trigger its state. (The checkbox cannot have a label at the left,
        /// that's why we need to use an external label for it.)
        /// </summary>
        private void labelStepByStep_Click(object sender, EventArgs e)
        {
            if (checkStep.Enabled) checkStep.Checked = !checkStep.Checked;
        }

        /// <summary>
        /// The label next to the checkbox should also trigger its state. (The checkbox cannot have a label at the left,
        /// that's why we need to use an external label for it.)
        /// </summary>
        private void labelShowInspector_Click_1(object sender, EventArgs e)
        {
            if (checkShowInspector.Enabled) checkShowInspector.Checked = !checkShowInspector.Checked;
        }

        /// <summary>
        /// The label next to the checkbox should also trigger its state. (The checkbox cannot have a label at the left,
        /// that's why we need to use an external label for it.)
        /// </summary>
        private void labelWriteToFile_Click(object sender, EventArgs e)
        {
            if (checkWriteToFile.Enabled) checkWriteToFile.Checked = !checkWriteToFile.Checked;
        }

        /// <summary>
        /// The label next to the checkbox should also trigger its state. (The checkbox cannot have a label at the left,
        /// that's why we need to use an external label for it.)
        /// </summary>
        private void labelRandomSeed_Click(object sender, EventArgs e)
        {
            if (checkRandomSeed.Enabled) checkRandomSeed.Checked = !checkRandomSeed.Checked;
        }
    }
}
