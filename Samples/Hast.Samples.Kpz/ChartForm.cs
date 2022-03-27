using Hast.Layer;
using Hast.Samples.Kpz.Algorithms;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Hast.Samples.Kpz;

/// <summary>The main form showing the log and the output graph of the algorithm.</summary>
public partial class ChartForm : Form
{
    public int NumKpzIterations => (int)nudIterations.Value;
    public int KpzWidth => (int)nudTableWidth.Value;
    public int KpzHeight => (int)nudTableHeight.Value;
    public bool ShowInspector => checkShowInspector.Checked;
    public bool WriteToFile => checkWriteToFile.Checked;
    public bool VerifyOutput => checkVerifyOutput.Checked;
    public bool StepByStep => checkStep.Checked;
    public bool RandomSeedEnable => checkRandomSeed.Checked;

    /// <summary>
    /// The BackgroundWorker is used to run the algorithm on a different CPU thread than the GUI,
    /// so that the GUI keeps responding while the algorithm is running.
    /// </summary>
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed in Closed event handler.")]
    private readonly BackgroundWorker _backgroundWorker;

    /// <summary>
    /// The Kpz object is used to perform the KPZ algorithm, to input its parameters and return the result.
    /// </summary>
    private Kpz _kpz;

    /// <summary>InspectForm allows us to inspect the results of the KPZ algorithm on a GUI interface.</summary>
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed in Closed event handler.")]
    private InspectForm _inspectForm;

    public ChartForm()
    {
        InitializeComponent();
        _backgroundWorker = new BackgroundWorker
        {
            WorkerSupportsCancellation = true,
        };
        _backgroundWorker.DoWork += BackgroundWorker_DoWork;
        _backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
        comboTarget.SelectedIndex = 4;
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
    private void ButtonStart_Click(object sender, EventArgs e)
    {
        panelTop.Enabled = false;

#pragma warning disable CA1303 // Do not pass literals as localized parameters. It's fine since we don't localize the rest either.
        buttonStart.Text = _backgroundWorker.IsBusy ? "Start" : "Stop";
#pragma warning restore CA1303 // Do not pass literals as localized parameters. It's fine since we don't localize the rest either.

        if (!_backgroundWorker.IsBusy)
        {
            progressBar.Maximum = NumKpzIterations;
            ComputationTarget = CurrentComputationTarget; // ComboBox value cannot be accessed from BackgroundWorker
            _backgroundWorker.RunWorkerAsync(2_000);
        }
        else
        {
            _backgroundWorker.CancelAsync();
        }
    }

    /// <summary>
    /// It runs the KPZ algorithm in the background.
    /// See also: <see cref="BackgroundWorker"/>.
    /// </summary>
    private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
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
    private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        if (e.Cancelled) LogIt("Operation was canceled.");
        else if (e.Error != null) LogIt($"An error occurred: {e.Error.Message}");
        else LogIt("Operation finished.");
        buttonStart.Enabled = false;
        progressBar.Visible = false;

        if (ShowInspector)
        {
            if (WriteToFile)
            {
                LogIt("Writing KpzStateLogger to file...");
                var kpzStateLoggerPath = Path.GetDirectoryName(
                    typeof(Program).Assembly.Location) + @"\kpzStateLogger\";
                if (!Directory.Exists(kpzStateLoggerPath)) Directory.CreateDirectory(kpzStateLoggerPath);
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
    /// One solution for this is using <c>Windows.Threading.Dispatcher.Invoke</c> to
    /// schedule an update in the GUI thread.
    /// AsyncLogIt schedules a <see cref="LogIt"/> operation on the GUI thread from within the
    /// <see cref="BackgroundWorker"/>.
    /// </summary>
    private void AsyncLogIt(string what) => Invoke(new Action(() => LogIt(what)));

    private void AsyncLogInvariant(FormattableString what) =>
        Invoke(new Action(() => LogIt(what.ToString(CultureInfo.InvariantCulture))));

    /// <summary>
    /// AsyncUpdateProgressBar schedules the progress bar value to be updated in GUI thread from within the
    /// <see cref="BackgroundWorker"/>. For more info, see <see cref="AsyncLogIt"/>.
    /// </summary>
    private void AsyncUpdateProgressBar(int progress) => Invoke(new Action(() => progressBar.Value = progress));

    /// <summary>
    /// AsyncUpdateChart schedules the chart to be updated in GUI thread from within the
    /// <see cref="BackgroundWorker"/>. For more info, see <see cref="AsyncLogIt"/>.
    /// </summary>
    private void AsyncUpdateChart(int iteration, bool forceUpdate = false)
    {
        // The chart is updated (and the statistics are calculated) 10 times in every logarithmic scale step, e.g.
        // in iterations: 1,2,3,4,5,6,7,8,9, 10,20,30,40,50,60,70,80,90, 100,200,300,400,500...
        int iterationNext10Pow = (int)Math.Pow(10, Math.Floor(Math.Log10(iteration) + 1));
        bool updateChartInThisIteartion = forceUpdate || iteration < 10 ||
            (iteration + 1) % (iterationNext10Pow / 10) == 0;

        if (updateChartInThisIteartion)
        {
            var meta = _kpz.GenerateAndProcessHeightMap();

            if (!meta.PeriodicityValid)
            {
                AsyncLogInvariant($"Warning: Periodicity invalid (x: {meta.PeriodicityInvalidXCount}, y: {meta.PeriodicityInvalidYCount})");
            }

            Invoke(new Action(() =>
            {
                LogIt(FormattableString.Invariant($"iteration: {iteration}, surfaceRoughness: {meta.StandardDeviation}"));
                chartKPZ.Series[0].Points.AddXY(iteration + 1, meta.StandardDeviation);
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
        _kpz = new Kpz(KpzWidth, KpzHeight, 0.5, 0.5, ShowInspector, ComputationTarget);
        AsyncLogIt("Filling grid with initial data...");
        _kpz.InitializeGrid();

        var (hastlayer, configuration) = ComputationTarget != KpzTarget.Cpu
            ? InitializeHastlayer()
            : (Hastlayer: null, Configuration: null);

        try
        {
            if (ComputationTarget == KpzTarget.PrngTest) return; // Already done test inside InitializeHastlayer

            var sw = Stopwatch.StartNew();
            AsyncLogIt("Starting KPZ iterations...");

            if (ComputationTarget.HastlayerParallelizedAlgorithm())
            {
                DoParallelIterations(hastlayer, configuration);
            }
            else if (!DoSingleIteration(hastlayer, configuration, bw))
            {
                return;
            }

            sw.Stop();
            AsyncLogInvariant($"Done. Total time measured: {sw.ElapsedMilliseconds} ms");
        }
        finally
        {
            hastlayer?.Dispose();
        }
    }

    private bool DoSingleIteration(IHastlayer hastlayer, IHardwareGenerationConfiguration configuration, BackgroundWorker bw)
    {
        for (int currentIteration = 0; currentIteration < NumKpzIterations; currentIteration++)
        {
            if (ComputationTarget == KpzTarget.Cpu)
            {
                _kpz.DoIteration();
            }
            else
            {
                if (StepByStep)
                {
                    _kpz.DoHastIterationDebug(hastlayer, configuration);
                }
                else
                {
                    _kpz.DoHastIterations(hastlayer, configuration, (uint)NumKpzIterations);
                    return true;
                }
            }

            AsyncUpdateProgressBar(currentIteration);
            AsyncUpdateChart(currentIteration);
            if (bw.CancellationPending) return false;
        }

        return true;
    }

    private void DoParallelIterations(IHastlayer hastlayer, IHardwareGenerationConfiguration configuration)
    {
        int currentIteration = 1;
        int lastIteration = 0;
        while (true)
        {
            int iterationsToDo = currentIteration - lastIteration;
            AsyncLogInvariant($"Doing {iterationsToDo} iterations at once...");
            _kpz.DoHastIterations(hastlayer, configuration, (uint)iterationsToDo);
            AsyncUpdateProgressBar(currentIteration);
            // Force update if current iteration is the last:
            AsyncUpdateChart(currentIteration - 1, currentIteration == NumKpzIterations);
            if (currentIteration >= NumKpzIterations) return;
            lastIteration = currentIteration;
            currentIteration *= 10;
            if (currentIteration > NumKpzIterations) currentIteration = NumKpzIterations;
        }
    }

    private (IHastlayer Hastlayer, IHardwareGenerationConfiguration Configuration) InitializeHastlayer()
    {
        AsyncLogIt("Initializing Hastlayer...");
        _kpz.LogItFunction = AsyncLogIt;
        var result = _kpz.InitializeHastlayerAsync(VerifyOutput, RandomSeedEnable).Result;

        result.Hastlayer.Invoking += (_, _) => AsyncLogIt("Hastlayer: Invoking member...");
        result.Hastlayer.ExecutedOnHardware += (_, e) => AsyncLogIt("Hastlayer: Executed member on hardware! " +
            FormattableString.Invariant($"(took {e.Arguments.HardwareExecutionInformation.FullExecutionTimeMilliseconds:0.000} ms)"));

        return result;
    }

    /// <summary>
    /// <see cref="labelShowInspector" /> is next to <see cref="checkShowInspector" />, and clicking on it
    /// can be used to toggle the checkbox.
    /// </summary>
    private void LabelShowInspector_Click(object sender, EventArgs e) => checkShowInspector.Checked = !checkShowInspector.Checked;

    private void ComboTarget_SelectedIndexChanged(object sender, EventArgs e)
    {
        nudTableWidth.Enabled = nudTableHeight.Enabled = comboTarget.SelectedIndex == 0;
        checkStep.Enabled = comboTarget.SelectedIndex != 0;
        checkVerifyOutput.Enabled = comboTarget.SelectedIndex is 2 or 4 or 5;
        if (comboTarget.SelectedIndex == 5) checkVerifyOutput.Checked = true;
        if (comboTarget.SelectedIndex is > 0 and <= 2)
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

    /// <summary>
    /// Gets the selected item in <see cref="comboTarget"/> to <see cref="KpzTarget"/> values.
    /// </summary>
    private KpzTarget CurrentComputationTarget =>
        comboTarget.SelectedIndex switch
        {
            0 => KpzTarget.Cpu,
            1 => KpzTarget.FpgaSimulation,
            2 => KpzTarget.Fpga,
            3 => KpzTarget.FpgaSimulationParallelized,
            4 => KpzTarget.FpgaParallelized,
            5 => KpzTarget.PrngTest,
            _ => KpzTarget.Cpu,
        };

    private KpzTarget ComputationTarget;

    /// <summary>
    /// The label next to the checkbox should also trigger its state. (The checkbox cannot have a label at the left,
    /// that's why we need to use an external label for it.)
    /// </summary>
    private void LabelVerifyOutput_Click(object sender, EventArgs e)
    {
        if (checkVerifyOutput.Enabled) checkVerifyOutput.Checked = !checkVerifyOutput.Checked;
    }

    /// <summary>
    /// The label next to the checkbox should also trigger its state. (The checkbox cannot have a label at the left,
    /// that's why we need to use an external label for it.)
    /// </summary>
    private void LabelStepByStep_Click(object sender, EventArgs e)
    {
        if (checkStep.Enabled) checkStep.Checked = !checkStep.Checked;
    }

    /// <summary>
    /// The label next to the checkbox should also trigger its state. (The checkbox cannot have a label at the left,
    /// that's why we need to use an external label for it.)
    /// </summary>
    private void LabelShowInspector_Click_1(object sender, EventArgs e)
    {
        if (checkShowInspector.Enabled) checkShowInspector.Checked = !checkShowInspector.Checked;
    }

    /// <summary>
    /// The label next to the checkbox should also trigger its state. (The checkbox cannot have a label at the left,
    /// that's why we need to use an external label for it.)
    /// </summary>
    private void LabelWriteToFile_Click(object sender, EventArgs e)
    {
        if (checkWriteToFile.Enabled) checkWriteToFile.Checked = !checkWriteToFile.Checked;
    }

    /// <summary>
    /// The label next to the checkbox should also trigger its state. (The checkbox cannot have a label at the left,
    /// that's why we need to use an external label for it.)
    /// </summary>
    private void LabelRandomSeed_Click(object sender, EventArgs e)
    {
        if (checkRandomSeed.Enabled) checkRandomSeed.Checked = !checkRandomSeed.Checked;
    }

    protected override void OnClosed(EventArgs e)
    {
        _backgroundWorker?.Dispose();
        _inspectForm?.Dispose();
        base.OnClosed(e);
    }
}
