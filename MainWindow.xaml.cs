using Kitware.VTK;
using System.Windows;
using System.Runtime.InteropServices;


namespace KasandraViewerVTK
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        private vtkActor? fullActor;
        private vtkActor? cutActor;
        private vtkScalarBarActor? scalarBar;
        private vtkPlane? cortePlane;
        private vtkCutter? cutter;
        private vtkDataSet? loadedDataSet;
        private vtkLookupTable? currentLUT;
        private vtkDataSetMapper? fullMapper;
        private vtkDataSet? currentDataSet;

        public MainWindow()
        {
            InitializeComponent();

            var renderWindowControl = new RenderWindowControl();
            vtkHost.Child = renderWindowControl;

            renderWindowControl.Load += (s, e) =>
            {
                var renderWindow = renderWindowControl.RenderWindow;
                var renderer = renderWindow.GetRenderers().GetFirstRenderer();

                renderer.SetBackground(0.1, 0.1, 0.1); // Color de fondo
            };
        }

        private void CargarArchivo_Click(object sender, RoutedEventArgs e)
        {
            var renderer = ((RenderWindowControl)vtkHost.Child).RenderWindow.GetRenderers().GetFirstRenderer();

            // 🧽 Eliminar del render
            if (fullActor != null) renderer.RemoveActor(fullActor);
            if (cutActor != null) renderer.RemoveActor(cutActor);
            if (scalarBar != null) renderer.RemoveActor2D(scalarBar);
            renderer.RemoveAllViewProps();
            renderer.GetRenderWindow().Render();

            // 🧹 Liberar manualmente los objetos VTK anteriores
            scalarBar?.Dispose();
            scalarBar?.Dispose();
            cutActor?.Dispose();
            fullActor?.Dispose();
            cutter?.Dispose();
            currentLUT?.Dispose();
            fullMapper?.Dispose();
            loadedDataSet?.Dispose();
            currentDataSet?.Dispose();

            // 🔄 Resetear referencias
            fullActor = null;
            cutActor = null;
            scalarBar = null;
            loadedDataSet = null;
            cutter = null;
            currentLUT = null;
            fullMapper = null;
            currentDataSet = null;

            // 🔁 Resetear el ComboBox de escalares
            ScalarsComboBox.Visibility = Visibility.Collapsed;
            ScalarsComboBox.Items.Clear();

            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "VTK Files (*.vtu;*.vtp)|*.vtu;*.vtp|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                renderer = ((RenderWindowControl)vtkHost.Child).RenderWindow.GetRenderers().GetFirstRenderer();
                renderer.RemoveAllViewProps();

                string extension = System.IO.Path.GetExtension(filePath).ToLower();

                vtkAlgorithmOutput? outputPort = null;

                if (extension == ".vtu")
                {
                    var reader = vtkXMLUnstructuredGridReader.New();
                    reader.SetFileName(filePath);
                    reader.Update();
                    loadedDataSet = reader.GetOutput();
                    outputPort = reader.GetOutputPort();

                    var unstructuredGrid = reader.GetOutput();

                    if (ContienePolyhedron(unstructuredGrid))
                    {
                        // Filtramos con surface directamente para evitar el cuelgue
                        Console.WriteLine("⚠️ Polyhedron encontrado. Usando surface como precaución.");

                        var surface = vtkDataSetSurfaceFilter.New();
                        surface.SetInputConnection(reader.GetOutputPort());
                        surface.Update();

                        cutter = vtkCutter.New();
                        cutter.SetCutFunction(cortePlane);
                        cutter.SetInputConnection(surface.GetOutputPort());

                        outputPort = surface.GetOutputPort();
                    }
                    else
                    {
                        cutter = vtkCutter.New();
                        cutter.SetCutFunction(cortePlane);
                        cutter.SetInputConnection(reader.GetOutputPort());
                    }

                    var mapper = vtkDataSetMapper.New();
                    mapper.SetInputConnection(outputPort);

                    fullActor = vtkActor.New();
                    fullActor.SetMapper(mapper);
                    renderer.AddActor(fullActor);
                }
                else if (extension == ".vtp")
                {
                    var reader = vtkXMLPolyDataReader.New();
                    reader.SetFileName(filePath);
                    reader.Update();

                    outputPort = reader.GetOutputPort();

                    var mapper = vtkPolyDataMapper.New();
                    mapper.SetInputConnection(outputPort);

                    fullActor = vtkActor.New();
                    fullActor.SetMapper(mapper);
                    renderer.AddActor(fullActor);
                }
                else
                {
                    System.Windows.MessageBox.Show("Formato de archivo no soportado.");
                    return;
                }

                // ✅ Crear plano
                cortePlane = vtkPlane.New();
                cortePlane.SetOrigin(0, 0, 0);
                cortePlane.SetNormal(0, 0, 1);

                // 🎯 Intentar cortar directamente el volumen
                cutter = vtkCutter.New();
                cutter.SetCutFunction(cortePlane);
                cutter.SetInputConnection(outputPort);

                renderer.SetBackground(0.1, 0.1, 0.1);
                renderer.ResetCamera();
                renderer.GetRenderWindow().Render();

                AllocConsole();
                Console.WriteLine($"Archivo cargado: {filePath}");
            }
        }

        private void VerCorte_Click(object sender, RoutedEventArgs e)
        {
            AllocConsole();
            Console.WriteLine("🔪 Activando vista de corte...");

            var renderer = ((RenderWindowControl)vtkHost.Child).RenderWindow.GetRenderers().GetFirstRenderer();
            renderer.RemoveAllViewProps();

            if (cutter == null || cortePlane == null)
            {
                Console.WriteLine("❌ Cutter o plano no inicializados.");
                return;
            }

            // 🔧 Actualizar corte
            cutter.Update();

            // 🔧 Geometría del corte
            var stripper = vtkStripper.New();
            stripper.SetInputConnection(cutter.GetOutputPort());
            stripper.Update();

            var triangleFilter = vtkTriangleFilter.New();
            triangleFilter.SetInputConnection(stripper.GetOutputPort());
            triangleFilter.Update();

            var cutMapper = vtkPolyDataMapper.New();
            cutMapper.SetInputConnection(triangleFilter.GetOutputPort());

            // 🎨 Si hay escalar activo, aplicamos color
            if (currentLUT != null && ScalarsComboBox.SelectedItem != null)
            {
                string selectedScalar = ScalarsComboBox.SelectedItem.ToString();
                cutMapper.SetScalarModeToUsePointFieldData();
                cutMapper.SelectColorArray(selectedScalar);
                cutMapper.SetScalarVisibility(1);

                double[] scalarRange = loadedDataSet?.GetPointData().GetArray(selectedScalar)?.GetRange();
                if (scalarRange != null)
                {
                    cutMapper.SetLookupTable(currentLUT);
                    cutMapper.SetScalarRange(scalarRange[0], scalarRange[1]);
                }

                // Volver a añadir la leyenda
                if (scalarBar != null)
                    renderer.AddActor2D(scalarBar);
            }

            cutActor = vtkActor.New();
            cutActor.SetMapper(cutMapper);
            cutActor.GetProperty().SetColor(1, 0, 0); // Por si no hay escalar

            renderer.AddActor(cutActor);
            renderer.ResetCamera();
            renderer.GetRenderWindow().Render();

            Console.WriteLine($"🎯 Celdas después del corte: {triangleFilter.GetOutput()?.GetNumberOfCells()}");
        }

        private bool ContienePolyhedron(vtkUnstructuredGrid grid)
        {
            int numCells = (int)grid.GetNumberOfCells();
            for (int i = 0; i < numCells; i++)
            {
                var cellType = grid.GetCellType(i);
                if (cellType == (int)vtkCellType.VTK_POLYHEDRON)
                {
                    Console.WriteLine($"⚠️ Celda VTK_POLYHEDRON detectada en índice {i}");
                    return true;
                }
            }
            return false;
        }

        private void ColorearPorEscalar_Click(object sender, RoutedEventArgs e)
        {
            if (fullActor == null || loadedDataSet == null)
            {
                System.Windows.MessageBox.Show("Primero carga un archivo .vtu");
                return;
            }

            var pointData = loadedDataSet.GetPointData();
            int numArrays = pointData.GetNumberOfArrays();

            if (numArrays == 0)
            {
                System.Windows.MessageBox.Show("No se encontraron escalares en el archivo.");
                return;
            }

            ScalarsComboBox.Items.Clear();
            for (int i = 0; i < numArrays; i++)
            {
                string name = pointData.GetArrayName(i);
                ScalarsComboBox.Items.Add(name);
            }

            ScalarsComboBox.Visibility = Visibility.Visible;

            if (numArrays == 1)
            {
                ScalarsComboBox.SelectedIndex = 0;
                ApplyScalarColoring(ScalarsComboBox.Items[0].ToString()!);
            }
            else
            {
                ScalarsComboBox.Visibility = Visibility.Visible;
                ScalarsComboBox.SelectedIndex = 0;
            }
        }

        private void AplicarThreshold_Click(object sender, RoutedEventArgs e)
        {
            if (loadedDataSet == null || ScalarsComboBox.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Primero selecciona un archivo y un escalar.");
                return;
            }

            string scalarName = ScalarsComboBox.SelectedItem.ToString();

            var pointData = loadedDataSet.GetPointData();
            var array = pointData.GetArray(scalarName);
            if (array == null)
            {
                System.Windows.MessageBox.Show("No se encontró el escalar seleccionado.");
                return;
            }

            double[] range = array.GetRange();
            double min = range[0];
            double max = range[1];

            // Puedes ajustar este rango si quieres filtrar más agresivamente
            double thresholdMin = min + (max - min) * 0.25;
            double thresholdMax = min + (max - min) * 0.75;

            // 1️⃣ Aplicar el filtro threshold
            var threshold = vtkThreshold.New();
            threshold.SetInputData(loadedDataSet);
            threshold.SetLowerThreshold(thresholdMin);
            threshold.SetUpperThreshold(thresholdMax);
            threshold.SetThresholdFunction(1); // 1 = BETWEEN
            threshold.SetInputArrayToProcess(0, 0, 0, 0, scalarName);
            threshold.Update();

            // 2️⃣ Convertir el resultado a PolyData (visualizable)
            var geometry = vtkGeometryFilter.New();
            geometry.SetInputConnection(threshold.GetOutputPort());
            geometry.Update();

            // 3️⃣ Crear el mapper y actor
            var mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(geometry.GetOutputPort());
            mapper.SetScalarModeToUsePointFieldData();
            mapper.SelectColorArray(scalarName);
            mapper.SetScalarVisibility(1);
            mapper.SetScalarRange(thresholdMin, thresholdMax);

            var actor = vtkActor.New();
            actor.SetMapper(mapper);

            // 4️⃣ Actualizar la escena
            var renderer = ((RenderWindowControl)vtkHost.Child).RenderWindow.GetRenderers().GetFirstRenderer();
            renderer.RemoveAllViewProps();

            renderer.AddActor(actor);
            renderer.ResetCamera();
            renderer.GetRenderWindow().Render();

            Console.WriteLine($"🎯 Threshold aplicado entre {thresholdMin} y {thresholdMax}");
        }

        private void ApplyScalarColoring(string scalarName)
        {
            var renderer = ((RenderWindowControl)vtkHost.Child).RenderWindow.GetRenderers().GetFirstRenderer();
            if (fullActor == null || loadedDataSet == null)
                return;

            var mapper = vtkDataSetMapper.New();
            mapper.SetInputData(loadedDataSet);
            mapper.SetScalarModeToUsePointFieldData();
            mapper.SelectColorArray(scalarName);
            mapper.ScalarVisibilityOn();

            var range = loadedDataSet.GetPointData().GetScalars(scalarName).GetRange();

            currentLUT = vtkLookupTable.New();
            currentLUT.SetNumberOfTableValues(256);
            currentLUT.SetRange(range[0], range[1]);
            currentLUT.Build();

            mapper.SetLookupTable(currentLUT);
            mapper.SetColorModeToMapScalars();

            fullActor.SetMapper(mapper);

            // Leyenda
            if (scalarBar != null)
                renderer.RemoveActor2D(scalarBar);

            scalarBar = vtkScalarBarActor.New();
            scalarBar.SetLookupTable(currentLUT);
            scalarBar.SetTitle(scalarName);
            scalarBar.SetNumberOfLabels(5);
            renderer.AddActor2D(scalarBar);

            renderer.ResetCamera();
            renderer.GetRenderWindow().Render();

            Console.WriteLine($"🎨 Aplicado escalar: {scalarName}");
        }

        //private void AplicarEscalar_Click(object sender, RoutedEventArgs e)
        //{
        //    if (loadedDataSet == null || ScalarsComboBox.SelectedItem == null)
        //        return;

        //    string selectedScalar = ScalarsComboBox.SelectedItem.ToString();

        //    var renderer = ((RenderWindowControl)vtkHost.Child).RenderWindow.GetRenderers().GetFirstRenderer();
        //    renderer.RemoveAllViewProps();

        //    // Mapper con escalar seleccionado
        //    var mapper = vtkDataSetMapper.New();
        //    mapper.SetInputData(loadedDataSet);
        //    mapper.SetScalarModeToUsePointFieldData();
        //    mapper.SelectColorArray(selectedScalar);
        //    mapper.SetScalarVisibility(1);

        //    // LookupTable
        //    currentLUT = vtkLookupTable.New();
        //    currentLUT.SetNumberOfTableValues(256);
        //    currentLUT.Build();

        //    double[] scalarRange = loadedDataSet.GetPointData().GetArray(selectedScalar).GetRange();
        //    mapper.SetLookupTable(currentLUT);
        //    mapper.SetScalarRange(scalarRange[0], scalarRange[1]);

        //    // Actor
        //    fullActor = vtkActor.New();
        //    fullActor.SetMapper(mapper);

        //    // Leyenda de colores
        //    scalarBar = vtkScalarBarActor.New();
        //    scalarBar.SetLookupTable(currentLUT);
        //    scalarBar.SetTitle(selectedScalar);
        //    scalarBar.SetNumberOfLabels(5);

        //    renderer.AddActor(fullActor);
        //    renderer.AddActor2D(scalarBar);
        //    renderer.ResetCamera();
        //    renderer.GetRenderWindow().Render();

        //    Console.WriteLine($"🎨 Escalar aplicado: {selectedScalar}");
        //}
        private void AplicarEscalar_Click(object sender, RoutedEventArgs e)
        {
            if (loadedDataSet == null || ScalarsComboBox.SelectedItem == null)
                return;

            string selectedScalar = ScalarsComboBox.SelectedItem.ToString();

            ApplyScalarColoring(selectedScalar);
        }

        private void ScalarsComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string selectedScalar = ScalarsComboBox.SelectedItem?.ToString() ?? "";
            if (!string.IsNullOrEmpty(selectedScalar))
                ApplyScalarColoring(selectedScalar);
        }

        private void CorteSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (cortePlane == null || cutActor == null) return;

            double z = e.NewValue;
            cortePlane.SetOrigin(0, 0, z);
            cutter.Update();

            Console.WriteLine($"Plano movido a Z={z}");

            ((RenderWindowControl)vtkHost.Child).RenderWindow.Render();

        }

        private void AbrirAnimacion_Click(object sender, RoutedEventArgs e)
        {
            var animWindow = new AnimacionEsferaWindow();
            animWindow.Show();
        }

        private void AbrirAnimacionFigura_Click(object sender, RoutedEventArgs e)
        {
            var animWin = new AnimacionFiguraWindow();
            animWin.Show();
        }
    }

    enum vtkCellType
    {
        VTK_POLYHEDRON = 42,
        VTK_HEXAHEDRON = 12,
        VTK_TETRA = 10,
        // Añade más tipos si necesitas otros
    }

}