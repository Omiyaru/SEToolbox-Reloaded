using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Input;

namespace SEToolbox.Controls
{
    public class AspectView : ItemsControl, INotifyPropertyChanged
    {
        public AspectCollection Aspects { get; set; }
        
        public AspectGroups AspectGroups { get; set; }
        
        public IAspectNodes IAspectNodes { get; set; }

        #region Dependency Properties

        public static readonly DependencyProperty AutoGenerateAspectsProperty =
            DependencyProperty.Register(
                nameof(AutoGenerateAspects),
                typeof(bool),
                typeof(AspectView),
                new PropertyMetadata(true, (d, e) => ((AspectView)d).RefreshAspects()));

        public bool AutoGenerateAspects
        {
            get => (bool)GetValue(AutoGenerateAspectsProperty);
			set => SetValue(AutoGenerateAspectsProperty, value);
        }

        public static readonly DependencyProperty ReflectionFallbackProperty =
            DependencyProperty.Register(
                nameof(ReflectionFallback),
                typeof(bool),
                typeof(AspectView),
                new PropertyMetadata(true, (d, e) => ((AspectView)d).RefreshAspects()));

        public bool ReflectionFallback
        {
            get => (bool)GetValue(ReflectionFallbackProperty);
			set => SetValue(ReflectionFallbackProperty, value);
        }

        public static readonly DependencyProperty GroupByCategoryProperty =
            DependencyProperty.Register(
                nameof(GroupByCategory),
                typeof(bool),
                typeof(AspectView),
                new PropertyMetadata(true, (d, e) => ((AspectView)d).RefreshAspects()));

        public bool GroupByCategory
        {
            get => (bool)GetValue(GroupByCategoryProperty);
			set => SetValue(GroupByCategoryProperty, value);
        }

        public static readonly DependencyProperty IncludeNonBrowsableProperty =
            DependencyProperty.Register(
                nameof(IncludeNonBrowsable),
                typeof(bool),
                typeof(AspectView),
                new PropertyMetadata(false, (d, e) => ((AspectView)d).RefreshAspects()));

        public bool IncludeNonBrowsable
        {
            get => (bool)GetValue(IncludeNonBrowsableProperty);
			set => SetValue(IncludeNonBrowsableProperty, value);
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly AutoTemplateSelector _templateSelector;
        private readonly ItemsControl _itemsControl;

        public AspectView()
        {

            AspectGroups = [];
            //IAspectNodes = (IAspectNodes)new AspectNodes();
            _itemsControl = new ListView();
            _templateSelector = new AutoTemplateSelector();
            BuildUI();
            _ = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            };
        }

        private void BuildUI()
        {
            if (Aspects == null) return;

            var expander = new Expander
            {
                Header = "Aspects",
                ExpandDirection = ExpandDirection.Down,
                IsExpanded = true,
                Content = _itemsControl,
            };

            _itemsControl.ItemTemplateSelector = _templateSelector;
            _itemsControl.AllowDrop = true;

            _itemsControl.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            _itemsControl.MouseMove += OnMouseMove;
            _itemsControl.DragOver += OnDragOver;
            _itemsControl.DragLeave += OnDragLeave;
            _itemsControl.Drop += OnDrop;

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = expander
            };
            _ = scrollViewer;
        }

        private Point _dragStartPoint;
        private ListViewItem _lastDropTarget;

        private void OnDragOver(object sender, DragEventArgs e)
        {
            var pos = e.GetPosition(_itemsControl);
            var element = _itemsControl.InputHitTest(pos) as DependencyObject;
            var item = ItemsControl.ContainerFromElement(_itemsControl, element) as ListViewItem;


            if (ReferenceEquals(_lastDropTarget, item)) 
            return;

            _lastDropTarget.SetValue(BackgroundProperty, null);

            if (item != null)
            {
                item.Background = Brushes.LightBlue; // Or any highlight color
                _lastDropTarget = item;
            }
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            if (_lastDropTarget != null)
            {
                _lastDropTarget.ClearValue(ListViewItem.BackgroundProperty);
                _lastDropTarget = null;
            }
        }
        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(null);
                if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if ((_itemsControl as Selector).SelectedItem is Aspect aspect)
                    {
                        DragDrop.DoDragDrop(_itemsControl, aspect, DragDropEffects.Move);
                    }
                }
            }
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(typeof(Aspect)))
                {
                    Aspect droppedData = e.Data.GetData(typeof(Aspect)) as Aspect;
                    if (((FrameworkElement)e.OriginalSource).DataContext is not Aspect target || droppedData == target)
                        return;

                    int removedIdx = Aspects.IndexOf(droppedData);
                    int targetIdx = Aspects.IndexOf(target);
                    if (removedIdx >= 0 && targetIdx >= 0 && removedIdx != targetIdx)
                    {
                        Aspects.Move(removedIdx, targetIdx);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnDrop: {ex.Message}");
            }
        }

        #region Aspect Refresh

        public void RefreshAspects()
        {
            Aspects.Clear();
            AspectGroups.Clear();

            var target = ResolveFirstItem(ItemsSource);
            if (target == null)
            {
                if (ReflectionFallback) Aspects.Clear();
                OnPropertyChanged(nameof(Aspects));
                return;
            }

            if (GroupByCategory)
            {
                AspectGroups.Clear();
                foreach (var group in GenerateAspectGroups(target))
                    AspectGroups.Add(group);
                _itemsControl.ItemsSource = AspectGroups;
            }
            else
            {
                Aspects.Clear();
                foreach (var aspect in GenerateAspects(target))
                    Aspects.Add(aspect);
                _itemsControl.ItemsSource = Aspects;
            }

            if (AutoGenerateAspects && AspectGroups.Count == 0 && Aspects.Count == 0)
            {
                if (GroupByCategory)
                {
                    AutoGenerateWithCategories(target);
                    AutoGenerateMissingWithCategories(target);
                }
                else
                {
                    AutoGenerateFlat(target);
                    AutoGenerateMissingFlat(target);
                }
            }

            OnPropertyChanged(nameof(Aspects));
        }

        private static object ResolveFirstItem(object source) =>
            source switch
            {
                null => null,
                string s => s,
                IEnumerable e => e.Cast<object>().FirstOrDefault(it => it != null),
                _ => source
            };

        #endregion

        #region Auto-Generation

        private void AutoGenerateFlat(object target)
        {
            Aspects.Clear();
            foreach (var aspect in GenerateAspects(target)) Aspects.Add(aspect);
        }

        private void AutoGenerateMissingFlat(object target)
        {
            var existingKeys = ExistingAspectKeys(Aspects);
            foreach (var aspect in GenerateAspects(target))
            {
                if (!existingKeys.Contains(AspectKey(aspect))) Aspects.Add(aspect);
            }
        }

        private void AutoGenerateWithCategories(object target)
        {
            AspectGroups.Clear();
            foreach (var group in GenerateAspectGroups(target))
                AspectGroups.Add(group);
        }

        private void AutoGenerateMissingWithCategories(object target)
        {
            var newGroups = GenerateAspectGroups(target).ToArray();
            var groupsByName = AspectGroups.ToDictionary(g => g.Header ?? "Misc");
            foreach (var newGroup in newGroups)
            {
                if (!groupsByName.TryGetValue(newGroup.Header ?? "Misc", out var existingGroup))
                {
                    AspectGroups.Add(newGroup);
                }
                else
                {
                    var existingKeys = ExistingAspectKeys(existingGroup);
                    foreach (var aspect in newGroup)
                    {
                        if (!existingKeys.Contains(AspectKey(aspect)))
                            existingGroup.Add(aspect);
                    }
                }
            }
        }

        #endregion

        #region Aspect Generation Helpers

        private static HashSet<string> ExistingAspectKeys(IEnumerable nodes)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var n in nodes)
            {
                if (n is Aspect a) keys.Add(AspectKey(a));
                if (n is AspectGroup g) foreach (var a2 in g) keys.Add(AspectKey(a2));
            }
            return keys;
        }

        private static string AspectKey(Aspect a) =>
            $"{a.Binding?.Source?.GetType().FullName ?? ""}:{a.Binding?.Path?.Path ?? a.Header ?? ""}";

        private IEnumerable<PropertyInfo> GetBrowsableProps(object target, bool includeNonBrowsable)
        {
            return target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && (includeNonBrowsable || p.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false));
        }

        private IEnumerable<Aspect> GenerateAspects(object target)
        {
            if (target == null) yield break;

            var props = GetBrowsableProps(target, IncludeNonBrowsable)
                .OrderBy(p => p.GetCustomAttribute<DisplayAttribute>()?.GetOrder() ?? int.MaxValue)
                .ThenBy(p => p.Name);

            foreach (var prop in props)
            {
                var (displayName, desc, readOnly) = ReadMeta(prop);
                yield return new Aspect
                {
                    Header = prop.Name,
                    Description = desc ?? prop.Name,
                    Category = prop.GetCustomAttribute<CategoryAttribute>()?.Category ?? "Misc",
                    IsReadOnly = readOnly,
                    Value = prop.GetValue(target),
                    AspectType = prop.PropertyType,
                    AspectInfo = prop,
                    Target = target
                };
            }
        }

        private IEnumerable<AspectGroup> GenerateAspectGroups(object target)
        {
            var groupedProps = GetBrowsableProps(target, IncludeNonBrowsable)
                .GroupBy(p => p.GetCustomAttribute<CategoryAttribute>()?.Category ?? "Misc")
                .OrderBy(g => g.Key);

            foreach (var group in groupedProps)
            {
                var aspectGroup = new AspectGroup(group.Key);
                foreach (var prop in group.OrderBy(p => p.GetCustomAttribute<DisplayAttribute>()?.GetOrder() ?? int.MaxValue).ThenBy(p => p.Name))
                {
                    var (displayName, desc, readOnly) = ReadMeta(prop);
                    aspectGroup.Add(new Aspect
                    {
                        Header = prop.Name,
                        Description = desc ?? prop.Name,
                        IsReadOnly = readOnly,
                        Value = prop.GetValue(target),
                        AspectType = prop.PropertyType,
                        AspectInfo = prop,
                        Target = target
                    });
                }
                yield return aspectGroup;
            }
        }

        private static (string displayName, string description, bool readOnly) ReadMeta(PropertyInfo p)
        {
            var dn = p.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
            var desc = p.GetCustomAttribute<DescriptionAttribute>()?.Description;
            var ro = p.GetCustomAttribute<ReadOnlyAttribute>()?.IsReadOnly ?? false;
            return (dn, desc, ro);
        }

        #endregion
    }

    public interface IAspectNodes : IList<IAspect> { }
    public class AspectNodes : ObservableCollection<IAspectNodes>, IAspect { }

    public interface IAspect
    {
        public interface IAspect
        {
            string Header { get; }
            string Description { get; }
            IEnumerable<IAspect> Children { get; }
            bool IsLeafNode { get; }
        }
    }

    public class AspectGroup(string header) : ObservableCollection<Aspect>
    {
        public string Header { get; set; } = header;
    }

    public class AspectGroups : ObservableCollection<AspectGroup>, IAspect
    {
        public new IEnumerable<AspectGroup> Items => this;
        public AspectGroups() : base()
        {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(Aspect)))
                {
                    Aspect aspect = (Aspect)Activator.CreateInstance(type);
                    AspectGroup aspectGroup = new(aspect.Header) { aspect };

                    Add(aspectGroup);
                }
            }
        }

        public object Header { get; internal set; }

        public IEnumerable<AspectGroup> Children => this;
        public bool IsLeafNode => true;
    }

    public class AspectCollection : ObservableCollection<Aspect>
    {
    }


    [ContentProperty(nameof(Value))]
    public class Aspect : UserControl, INotifyPropertyChanged, IAspect
    {
        public string Header { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IEnumerable<IAspect> Children => [];
        public bool IsLeafNode => true;
        public Binding Binding { get; set; } = null;
        public string BindingPath { get; set; } = string.Empty;
        public Type EditorType { get; set; } = null;
        public string Category { get; set; } = string.Empty;
        public DataTemplate DataTemplate { get; set; } = null;
        public static Typeface Font { get; set; } = new Typeface("Segoe UI");
        public bool IsReadOnly { get; set; } = false;
        public Type AspectType { get; set; } = null;
        public PropertyInfo AspectInfo { get; set; }
        
        public TextAlignment TextAlignment { get; private set; }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(object),
                typeof(Aspect),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public object Value
        {
            get => GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
        }
        
        public object Target { get; set; }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var aspect = (Aspect)d;
            if (aspect.AspectInfo != null && aspect.Target != null && !aspect.IsReadOnly)
            {
                try
                {
                    aspect.AspectInfo.SetValue(aspect.Target, e.NewValue);
                }
                catch (Exception)
                {

                }
            }
            aspect.OnPropertyChanged(nameof(Value));
        }

        public Aspect()
        {
            Header = string.Empty;
            Description = string.Empty;
            Value = string.Empty;
            BindingPath = string.Empty;
            Category = string.Empty;
            EditorType = null;
            DataTemplate = null;
            AspectInfo = null;
            TextAlignment = TextAlignment.Left;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
            Target = null;
            ToolTip = null;
            Font = new Typeface("Segoe UI");
            IsReadOnly = false;
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class AutoTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container) =>
            item switch
            {
                { } control => (DataTemplate)Application.Current.FindResource($"{control.GetType().Name}Template"),
                _ => null
            };
    }

    public class GroupAwareTemplateSelector : DataTemplateSelector
    {
        private static readonly Dictionary<Type, Type> EditorControlMap = typeof(AspectView).GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(Dictionary<Type, Type>))
            .Select(f => (Dictionary<Type, Type>)f.GetValue(null))
            .First();

        private static readonly Dictionary<string, Action<FrameworkElementFactory, Aspect>> propertyBindingMap =
            typeof(AspectView).GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.ReturnType == typeof(Action<FrameworkElementFactory, Aspect>))
            .ToDictionary(m => m.Name, m => (Action<FrameworkElementFactory, Aspect>)m.CreateDelegate(null, typeof(Action<FrameworkElementFactory, Aspect>)));


        public static Func<Type, Aspect, DataTemplate> UnknownTypeResolver { get; set; }

        private static Type ResolveControlType(Type type)
        {
            return EditorControlMap.TryGetValue(type, out var controlType) ? controlType :
                type.IsEnum ? typeof(ComboBox) :
                IsNumericType(type) || IsNumericNull(type) ? typeof(TextBox) :
                IsBooleanType(type) ? typeof(CheckBox) :
                typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string) ? typeof(ListBox) :
                type.IsClass && type != typeof(string) ? typeof(Expander) :
                (TryGetEditorAttributeType(type) ?? EditorControlMap.Values.FirstOrDefault(x => x != null));

        }


        private static DataTemplate CreateGroupTemplate(string groupNamePath, DataTemplateSelector reUseSelector)
        {
            var expander = new FrameworkElementFactory(typeof(Expander));
            expander.SetBinding(HeaderedContentControl.HeaderProperty, new Binding(groupNamePath));
            var innerItems = new FrameworkElementFactory(typeof(ItemsControl));
            innerItems.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("."));
            innerItems.SetValue(ItemsControl.ItemTemplateSelectorProperty, reUseSelector);
            expander.AppendChild(innerItems);
            return new DataTemplate { VisualTree = expander };
        }

        private static DataTemplate CreateBindingTemplate(Type controlType, Aspect aspect)
        {
            var template = new DataTemplate();
            var f = new FrameworkElementFactory(controlType);
            if (propertyBindingMap.TryGetValue(controlType.Name, out var bindingAction))
                bindingAction(f, aspect);
            else
                // Fallback for custom controls
                f.SetBinding(ContentControl.ContentProperty, new Binding(nameof(Aspect.Value)) { Source = aspect });
            template.VisualTree = f;
            return template;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var type = item?.GetType();
            var resolvedType = ResolveControlType(type);
            var controlType = EditorControlMap.Values.FirstOrDefault(x => x != null);
            return item switch
            {
                AspectGroup aspectGroup => CreateGroupTemplate(nameof(AspectGroup.Header), this),
                Aspect aspect when aspect.DataTemplate != null => aspect.DataTemplate,
                Aspect aspect when EditorControlMap.Values.Any(x => x == null) => CreateBindingTemplate(EditorControlMap.Keys.FirstOrDefault(x => x.IsAssignableFrom(aspect.GetType())), aspect),
                Aspect aspect when type == aspect.AspectType || type == aspect.Value?.GetType() => CreateBindingTemplate(type, aspect),
                Aspect aspect when type == typeof(string) => CreateBindingTemplate(typeof(TextBlock), aspect),
                Aspect aspect when EditorControlMap.TryGetValue(resolvedType, out controlType) => CreateBindingTemplate(resolvedType, aspect),
                Aspect aspect when EditorControlMap.TryGetValue(type, out controlType) => CreateBindingTemplate(controlType, aspect),
                Aspect aspect => SmartFallback(aspect, type),
                _ => base.SelectTemplate(item, container)
            };
        }

        private static DataTemplate SmartFallback(Aspect aspect, Type type)
        {
            if (type.IsClass && type != typeof(string))
            {
                return CreateExpandableObjectTemplate(aspect);
            }

            return UnknownTypeResolver?.Invoke(type, aspect) ??
                   CreateBindingTemplate(typeof(TextBlock), aspect);
        }

        private static DataTemplate CreateExpandableObjectTemplate(Aspect aspect)
        {
            var template = new DataTemplate();
            var f = new FrameworkElementFactory(typeof(Expander));
            f.SetValue(Expander.IsExpandedProperty, false);
            f.SetBinding(HeaderedContentControl.HeaderProperty, new Binding(nameof(Aspect.Header)) { Source = aspect });
            var innerItems = new FrameworkElementFactory(typeof(ItemsControl));
            innerItems.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(Aspect.Value)) { Source = aspect });
            innerItems.SetValue(ItemsControl.ItemTemplateSelectorProperty, new GroupAwareTemplateSelector());
            var panel = new FrameworkElementFactory(typeof(StackPanel));
            panel.AppendChild(innerItems);
            f.AppendChild(panel);
            template.VisualTree = f;
            return template;
        }

        private static Type TryGetEditorAttributeType(Type type)
        {
            if (type.GetCustomAttributes(typeof(EditorAttribute), true).FirstOrDefault() is not EditorAttribute editorAttr)
                return null;

            return Type.GetType(editorAttr.EditorTypeName ?? string.Empty);
        }

        #region Type Helpers
        private static readonly HashSet<Type> _booleanTypes = [typeof(bool), typeof(bool?)];
        private static bool IsBooleanType(Type t) => _booleanTypes.Contains(t);

        private static readonly HashSet<Type> _numericTypes = [.. Enum.GetValues(typeof(TypeCode))
            .Cast<TypeCode>()
            .Select(t => Type.GetType(t.ToString()))
            .ToLookup(t => t, t => t).Where(g => g.Count() == 1).SelectMany(g => g)];

        private static bool IsNumericType(Type t) => _numericTypes.TryGetValue(t, out _);

        private static bool IsNumericNull(Type valueType)
        {
            return
            valueType.IsValueType &&
            valueType.IsGenericType &&
            valueType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
            IsNumericType(Nullable.GetUnderlyingType(valueType));
        }

        #endregion

        public void SetCustomTemplate(IEnumerable<Aspect> aspects, string propertyName, DataTemplate template)
        {
            foreach (var aspect in aspects.Where(a => a.Header == propertyName))
                aspect.DataTemplate = template;
        }

    }

    #region Custom Framework Elements
    public class ImageBox : Image, IUriContext
    {
        public ComboBox DType { get; set; }
        
        public List<string> Items { get; set; }
    }
    #endregion
}