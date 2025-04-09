using Kitware.VTK;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace KasandraViewerVTK
{
    /// <summary>
    /// Lógica de interacción para AnimacionFiguraWindow.xaml
    /// </summary>
    /// 
    public partial class AnimacionFiguraWindow : Window
    {
        private string[] vtuFiles;
        private int currentIndex = 0;
        private DispatcherTimer timer;
        private vtkActor? actor;
        private vtkRenderer? renderer;
        private vtkXMLUnstructuredGridReader reader;

        public AnimacionFiguraWindow()
        {
            InitializeComponent();

            var renderControl = new RenderWindowControl();
            vtkHostAnim.Child = renderControl;

            renderControl.Load += (s, e) =>
            {
                string folderPath = @"C:\Users\fujit\Codigo\KasandraViewerVTK\Datos\Llenado180M\VTK\Llenado180M_0\";
                vtuFiles = Directory.GetFiles(folderPath, "internal*.vtu").OrderBy(f => f).ToArray();

                if (vtuFiles.Length == 0)
                {
                    System.Windows.MessageBox.Show("No se encontraron archivos .vtu.");
                    return;
                }

                var renderWindow = renderControl.RenderWindow;
                renderer = renderWindow.GetRenderers().GetFirstRenderer();
                renderer.SetBackground(0.1, 0.1, 0.1);

                reader = vtkXMLUnstructuredGridReader.New();
                reader.SetFileName(vtuFiles[0]);
                reader.Update();

                var mapper = vtkDataSetMapper.New();
                mapper.SetInputConnection(reader.GetOutputPort());

                actor = vtkActor.New();
                actor.SetMapper(mapper);
                renderer.AddActor(actor);
                renderer.ResetCamera();
                renderWindow.Render();

                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(300);
                timer.Tick += (s, e) => MostrarSiguientePaso();
                timer.Start();
            };
        }

        private void MostrarSiguientePaso()
        {
            if (vtuFiles.Length == 0 || actor == null || renderer == null)
                return;

            currentIndex = (currentIndex + 1) % vtuFiles.Length;

            reader.SetFileName(vtuFiles[currentIndex]);
            reader.Update();

            vtkHostAnim.Child?.Invalidate(); // refresca la ventana
            ((RenderWindowControl)vtkHostAnim.Child).RenderWindow.Render();
        }
    }
    //public partial class AnimacionFiguraWindow : Window
    //{
    //    private vtkPVDReader? pvdReader;
    //    private vtkActor? actor;
    //    private DispatcherTimer? timer;
    //    private int currentTimestep = 0;
    //    private int totalTimesteps = 12;

    //    public AnimacionFiguraWindow()
    //    {
    //        InitializeComponent();

    //        var renderControl = new RenderWindowControl();
    //        vtkHostAnim.Child = renderControl;

    //        renderControl.Load += (s, e) =>
    //        {
    //            var renderWindow = renderControl.RenderWindow;
    //            var renderer = renderWindow.GetRenderers().GetFirstRenderer();

    //            string filePath = @"C:\Users\fujit\Codigo\KasandraViewerVTK\Datos\FiguraVideo\FiguraVideo.pvd";

    //            pvdReader = vtkPVDReader.New();
    //            pvdReader.SetFileName(filePath);
    //            pvdReader.Update();

    //            var mapper = vtkDataSetMapper.New();
    //            mapper.SetInputConnection(pvdReader.GetOutputPort());

    //            actor = vtkActor.New();
    //            actor.SetMapper(mapper);

    //            renderer.AddActor(actor);
    //            renderer.SetBackground(0.1, 0.1, 0.1);
    //            renderer.ResetCamera();
    //            renderWindow.Render();

    //            // 🕒 Timer
    //            timer = new DispatcherTimer();
    //            timer.Interval = TimeSpan.FromMilliseconds(300);
    //            timer.Tick += (s, e) =>
    //            {
    //                if (pvdReader == null || actor == null) return;

    //                pvdReader.SetTimeStep(currentTimestep);
    //                pvdReader.Update();

    //                renderWindow.Render();

    //                currentTimestep++;
    //                if (currentTimestep >= totalTimesteps)
    //                    currentTimestep = 0;
    //            };
    //            timer.Start();
    //        };
    //    }
    //}

}
