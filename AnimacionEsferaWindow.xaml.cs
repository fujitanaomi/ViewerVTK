using Kitware.VTK;
using System;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace KasandraViewerVTK
{
    /// <summary>
    /// Lógica de interacción para AnimacionEsferaWindow.xaml
    /// </summary>
    public partial class AnimacionEsferaWindow : Window
    {
        private vtkSphereSource? esfera;
        private DispatcherTimer? timer;
        private double radio = 1.0;
        public AnimacionEsferaWindow()
        {
            InitializeComponent();

            var renderControl = new RenderWindowControl();
            vtkHostAnimacion.Child = renderControl;

            renderControl.Load += (s, e) =>
            {
                var renderWindow = renderControl.RenderWindow;
                var renderer = renderWindow.GetRenderers().GetFirstRenderer();

                esfera = vtkSphereSource.New();
                esfera.SetRadius(radio);
                esfera.SetThetaResolution(30);
                esfera.SetPhiResolution(30);
                esfera.Update();

                var mapper = vtkPolyDataMapper.New();
                mapper.SetInputConnection(esfera.GetOutputPort());

                var actor = vtkActor.New();
                actor.SetMapper(mapper);
                actor.GetProperty().SetColor(0.2, 0.7, 1.0); // azul

                renderer.AddActor(actor);
                renderer.SetBackground(0.1, 0.1, 0.1);
                renderer.ResetCamera();
                renderWindow.Render();

                // 🎞️ Timer para animar
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(100);
                timer.Tick += (s, e) =>
                {
                    radio += 0.1;
                    if (radio > 5.0) radio = 1.0;
                    esfera.SetRadius(radio);
                    esfera.Update();
                    renderWindow.Render();
                };
                timer.Start();
            };
        }
    }
}
