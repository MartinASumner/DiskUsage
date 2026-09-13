using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DiskUsage.Models;
using DiskUsage.Services;
using DiskUsage.ViewModels;

namespace DiskUsage.Views
{
    public class TreemapControl : FrameworkElement
    {
        private static readonly Typeface BoldTypeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        private static readonly Typeface RegularTypeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        private readonly ITreemapLayoutEngine _layoutEngine = new TreemapLayoutEngine();
        private IReadOnlyList<TreemapNode> _nodes = Array.Empty<TreemapNode>();
        private TreemapNode? _hoveredNode;
        private bool _isLayoutScheduled;

        private readonly Pen _borderPen;
        private readonly Pen _hoverPen;
        private readonly Pen _selectedPen;
        private readonly Brush _backgroundBrush;
        private readonly Brush _textBrush;
        private readonly Brush _secondaryTextBrush;

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable<FileSystemItemViewModel>),
                typeof(TreemapControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnItemsSourceChanged));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(FileSystemItemViewModel),
                typeof(TreemapControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender, OnSelectedItemChanged));

        public static readonly DependencyProperty DrillDownCommandProperty =
            DependencyProperty.Register(
                nameof(DrillDownCommand),
                typeof(ICommand),
                typeof(TreemapControl),
                new PropertyMetadata(null));

        public IEnumerable<FileSystemItemViewModel>? ItemsSource
        {
            get => (IEnumerable<FileSystemItemViewModel>?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public FileSystemItemViewModel? SelectedItem
        {
            get => (FileSystemItemViewModel?)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public ICommand? DrillDownCommand
        {
            get => (ICommand?)GetValue(DrillDownCommandProperty);
            set => SetValue(DrillDownCommandProperty, value);
        }

        public TreemapControl()
        {
            ClipToBounds = true;
            Focusable = true;

            _backgroundBrush = new SolidColorBrush(Color.FromRgb(30, 33, 38));
            _backgroundBrush.Freeze();

            _borderPen = new Pen(new SolidColorBrush(Color.FromArgb(140, 20, 20, 20)), 1);
            _borderPen.Freeze();

            _hoverPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 255)), 2);
            _hoverPen.Freeze();

            _selectedPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 215, 0)), 2.5);
            _selectedPen.Freeze();

            _textBrush = Brushes.White;
            _secondaryTextBrush = new SolidColorBrush(Color.FromRgb(220, 225, 230));
            _secondaryTextBrush.Freeze();

            Loaded += (s, e) => ScheduleRecalculateLayout();
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    ScheduleRecalculateLayout();
                }
            };

            SizeChanged += OnSizeChanged;
            MouseMove += OnMouseMove;
            MouseLeave += OnMouseLeave;
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseRightButtonDown += OnMouseRightButtonDown;
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreemapControl control)
            {
                if (e.OldValue is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= control.OnItemsSourceCollectionChanged;
                }

                if (e.NewValue is INotifyCollectionChanged newCollection)
                {
                    newCollection.CollectionChanged += control.OnItemsSourceCollectionChanged;
                }

                control.ScheduleRecalculateLayout();
            }
        }

        private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ScheduleRecalculateLayout();
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreemapControl control)
            {
                control.InvalidateVisual();
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RecalculateLayout();
        }

        private void ScheduleRecalculateLayout()
        {
            if (_isLayoutScheduled)
            {
                return;
            }

            _isLayoutScheduled = true;
            Dispatcher.InvokeAsync(() =>
            {
                _isLayoutScheduled = false;
                RecalculateLayout();
            }, DispatcherPriority.Loaded);
        }

        private void RecalculateLayout()
        {
            if (ActualWidth <= 0 || ActualHeight <= 0 || ItemsSource == null)
            {
                _nodes = Array.Empty<TreemapNode>();
                InvalidateVisual();
                return;
            }

            var itemsList = new List<FileSystemItemViewModel>();
            foreach (var item in ItemsSource)
            {
                if (item != null) itemsList.Add(item);
            }

            var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
            _nodes = _layoutEngine.ComputeLayout(itemsList, bounds);
            InvalidateVisual();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(this);
            var hit = HitTestNode(pos);

            Cursor = (hit != null && hit.IsDirectory) ? Cursors.Hand : Cursors.Arrow;

            if (!ReferenceEquals(hit, _hoveredNode))
            {
                _hoveredNode = hit;
                UpdateTooltip(hit);
                InvalidateVisual();
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Arrow;
            if (_hoveredNode != null)
            {
                _hoveredNode = null;
                ToolTip = null;
                InvalidateVisual();
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(this);
            var hit = HitTestNode(pos);

            if (hit != null)
            {
                SelectedItem = hit.Item;

                if (hit.IsDirectory)
                {
                    if (DrillDownCommand != null && DrillDownCommand.CanExecute(hit.Item))
                    {
                        DrillDownCommand.Execute(hit.Item);
                    }
                }
            }
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(this);
            var hit = HitTestNode(pos);

            if (hit != null)
            {
                SelectedItem = hit.Item;
            }
        }

        private TreemapNode? HitTestNode(Point point)
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].Bounds.Contains(point))
                {
                    return _nodes[i];
                }
            }
            return null;
        }

        private void UpdateTooltip(TreemapNode? node)
        {
            if (node == null)
            {
                ToolTip = null;
                return;
            }

            var panel = new StackPanel { Margin = new Thickness(4) };

            var header = new TextBlock
            {
                Text = $"{(node.IsDirectory ? "📁" : "📄")} {node.Name}",
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 4)
            };
            panel.Children.Add(header);

            panel.Children.Add(new TextBlock { Text = $"Category: {node.Category}", FontSize = 11, Foreground = Brushes.LightGray });
            panel.Children.Add(new TextBlock { Text = $"Size: {node.FormattedSize} ({node.PercentOfParentText} of parent)", FontWeight = FontWeights.SemiBold, FontSize = 12 });
            panel.Children.Add(new TextBlock { Text = $"Path: {node.FullPath}", FontSize = 10, Foreground = Brushes.Silver, TextWrapping = TextWrapping.Wrap, MaxWidth = 350 });

            if (node.IsDirectory)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "💡 Click to focus into this folder",
                    FontSize = 11,
                    Foreground = Brushes.SkyBlue,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }

            ToolTip = new ToolTip
            {
                Content = panel,
                Background = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8)
            };
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
            dc.DrawRectangle(_backgroundBrush, null, bounds);

            if (_nodes.Count == 0)
            {
                var prompt = new FormattedText(
                    "No items or scan in progress...",
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    RegularTypeface,
                    14,
                    Brushes.Gray,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                var point = new Point((ActualWidth - prompt.Width) / 2, (ActualHeight - prompt.Height) / 2);
                dc.DrawText(prompt, point);
                return;
            }

            // Draw all tiles
            foreach (var node in _nodes)
            {
                if (node.Bounds.Width <= 0 || node.Bounds.Height <= 0) continue;

                dc.DrawRectangle(node.FillBrush, _borderPen, node.Bounds);

                // Draw Text labels if tile is large enough
                if (node.IsLargeEnoughForText)
                {
                    dc.PushClip(new RectangleGeometry(node.Bounds));

                    double padding = 4;
                    double availableWidth = Math.Max(0, node.Bounds.Width - (padding * 2));
                    double availableHeight = Math.Max(0, node.Bounds.Height - (padding * 2));

                    if (availableWidth > 20 && availableHeight > 12)
                    {
                        var nameText = new FormattedText(
                            node.Name,
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            BoldTypeface,
                            11,
                            _textBrush,
                            VisualTreeHelper.GetDpi(this).PixelsPerDip)
                        {
                            MaxTextWidth = availableWidth,
                            MaxTextHeight = 16,
                            Trimming = TextTrimming.CharacterEllipsis
                        };

                        dc.DrawText(nameText, new Point(node.Bounds.X + padding, node.Bounds.Y + padding));

                        if (node.IsLargeEnoughForFullDetails && availableHeight >= 28)
                        {
                            var sizeText = new FormattedText(
                                node.FormattedSize,
                                CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                RegularTypeface,
                                10,
                                _secondaryTextBrush,
                                VisualTreeHelper.GetDpi(this).PixelsPerDip)
                            {
                                MaxTextWidth = availableWidth,
                                MaxTextHeight = 14,
                                Trimming = TextTrimming.CharacterEllipsis
                            };

                            dc.DrawText(sizeText, new Point(node.Bounds.X + padding, node.Bounds.Y + padding + 16));
                        }
                    }

                    dc.Pop(); // Pop clip
                }
            }

            // Draw selection highlight over selected node
            if (SelectedItem != null)
            {
                foreach (var node in _nodes)
                {
                    if (ReferenceEquals(node.Item, SelectedItem))
                    {
                        var selRect = new Rect(node.Bounds.X + 1, node.Bounds.Y + 1, Math.Max(0, node.Bounds.Width - 2), Math.Max(0, node.Bounds.Height - 2));
                        dc.DrawRectangle(null, _selectedPen, selRect);
                        break;
                    }
                }
            }

            // Draw hover highlight
            if (_hoveredNode != null)
            {
                var hoverRect = new Rect(_hoveredNode.Bounds.X + 1, _hoveredNode.Bounds.Y + 1, Math.Max(0, _hoveredNode.Bounds.Width - 2), Math.Max(0, _hoveredNode.Bounds.Height - 2));
                dc.DrawRectangle(null, _hoverPen, hoverRect);
            }
        }
    }
}
