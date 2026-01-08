
using System.Windows;
using SEToolbox.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using SEToolbox.Interop;
using SEToolbox.Models;
using VRageMath;
using System;
using System.Windows.Controls;
using System.Collections.Generic;
namespace SEToolbox.Views
{
    public partial class Voxel3DViewer : UserControl
    {
        private bool isSliceView = true;
        private bool showMaterial = true;
        private bool showChunkGrid = true;
        private int currentZ = 0;
        private readonly Model3DGroup sliceVisuals = new();
        private readonly Model3DGroup volumeVisuals = new();
        private readonly Model3DGroup chunkGridLines = new();
        private VoxelGridModel voxelGrid;
        private MaterialPalette palette;
        private readonly object volumeVisualsContainer;

        public Voxel3DViewer(object volumeVisualsContainer, object sliceVisualsContainer)
        {
            this.volumeVisualsContainer = volumeVisualsContainer;
            this.sliceVisualsContainer = sliceVisualsContainer;
        }

        private readonly object sliceVisualsContainer;

        public Voxel3DViewer()
        {
            InitializeComponent();
            // Assuming SliceVisuals, VolumeVisuals, and ChunkGridLines are defined in XAML.
            ModelVisual3D sliceVisualsContainer = (ModelVisual3D)FindName("Slice Navigation:");
            ModelVisual3D volumeVisualsContainer = (ModelVisual3D)FindName("Material Rendering:");
            ModelVisual3D chunkGridLinesContainer = (ModelVisual3D)FindName("ChunkGridLines");
            sliceVisualsContainer.Content = sliceVisuals;
            volumeVisualsContainer.Content = volumeVisuals;
            chunkGridLinesContainer.Content = chunkGridLines;
            HelixViewport3D viewport = (HelixViewport3D)FindName("HelixViewport3D");
            Loaded += (s, e) => viewport.ZoomExtents();
            KeyDown += OnKeyDown;
        }

        public void LoadVoxelData(VoxelGridModel grid, MaterialPalette matPalette)
        {
            voxelGrid = grid;
            palette = matPalette;
            RenderSlice();
            RenderFullVolume();
            RenderChunkGrid();
            UpdateView();
        }

        public string GetViewName()
        {
            return "Voxel 3D";
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                currentZ = Math.Min(currentZ + 1, voxelGrid.SizeZ - 1);
                RenderSlice();
            }
            else if (e.Key == Key.Down)
            {
                currentZ = Math.Max(currentZ - 1, 0);
                RenderSlice();
            }
            else if (e.Key == Key.V)
            {
                isSliceView = !isSliceView;
                UpdateView();
            }
            else if (e.Key == Key.M)
            {
                showMaterial = !showMaterial;
                RenderSlice();
                RenderFullVolume();
            }
            else if (e.Key == Key.C)
            {
                showChunkGrid = !showChunkGrid;
                chunkGridLines.Children.Clear();
                if (showChunkGrid) RenderChunkGrid();
            }
            else if (e.Key == Key.R)
            {
                HelixViewport3D viewport = (HelixViewport3D)FindName("HelixViewport3D");
                viewport.ZoomExtents();
            }
            UpdateStatus();
        }

        private void UpdateView()
        {
            if (isSliceView) RenderSlice();
            ((UIElement)sliceVisualsContainer).Visibility = isSliceView ? Visibility.Visible : Visibility.Hidden;
            ((UIElement)volumeVisualsContainer).Visibility = isSliceView ? Visibility.Hidden : Visibility.Visible;
        }
        
        private void UpdateStatus()
        {
            TextBlock statusText = (TextBlock)FindName("StatusText");
            statusText.Text = $"Mode: {(isSliceView ? "Slice" : "Volume")} | Layer: {currentZ}";
        }
        
        private void RenderSlice()
        {
            sliceVisuals.Children.Clear();
            for (int x = 0; x < voxelGrid.SizeX; x++)
                for (int y = 0; y < voxelGrid.SizeY; y++)
                {
                    _ = voxelGrid.GetMaterial(x, y, currentZ);
                    var val = voxelGrid.GetContent(x, y, currentZ);
                    if (val == 0) continue;

                    var color = showMaterial ? System.Windows.Media.Color.FromArgb(255, 255, 255, 255) : System.Windows.Media.Color.FromArgb(val, 255, 255, 255);

                    var box = CreateVoxelCube(x, y, currentZ, color);
                    sliceVisuals.Children.Add(box);
                }
        }

        private void RenderFullVolume()
        {
            volumeVisuals.Children.Clear();

            int step = 2; // skip every 2 voxels for performance
            for (int z = 0; z < voxelGrid.SizeZ; z += step)
                for (int y = 0; y < voxelGrid.SizeY; y += step)
                    for (int x = 0; x < voxelGrid.SizeX; x += step)
                    {
                        var val = voxelGrid.GetContent(x, y, z);
                        if (val == 0) continue;
                        _ = voxelGrid.GetMaterial(x, y, z);
                        var color = showMaterial ? System.Windows.Media.Color.FromArgb(255, 255, 255, 255) : System.Windows.Media.Color.FromArgb(val, 255, 255, 255);

                        color.A = 100; // translucent in volume mode
                        var box = CreateVoxelCube(x, y, z, color);
                        volumeVisuals.Children.Add(box);
                    }
        }

        private void RenderChunkGrid()
        {
            chunkGridLines.Children.Clear();

            var sizeX = voxelGrid.SizeX;
            var sizeY = voxelGrid.SizeY;
            var sizeZ = voxelGrid.SizeZ;
            var grid = new LinesVisual3D { Thickness = 0.3, Color = Colors.Red };

            for (int x = 0; x <= sizeX; x += 16)
            {
                for (int y = 0; y <= sizeY; y += 16)
                {
                    grid.Points.Add(new Point3D(x, y, 0));
                    grid.Points.Add(new Point3D(x, y, sizeZ));
                }
            }

            chunkGridLines.Children.Add(grid.Content);
        }

        private Model3D CreateVoxelCube(int x, int y, int z, System.Windows.Media.Color color)
        {
            var mat = MaterialHelper.CreateMaterial(new SolidColorBrush(color));
            return new BoxVisual3D
            {
                Center = new Point3D(x + 0.5, y + 0.5, z + 0.5),
                Width = 1,
                Height = 1,
                Length = 1,
                Material = mat
            }.Model;
        }
    }
}


