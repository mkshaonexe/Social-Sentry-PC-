using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Social_Sentry.ViewModels;

namespace Social_Sentry.Views
{
    public partial class CategoryView : System.Windows.Controls.UserControl
    {
        public CategoryView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var vm = DataContext as CategoryViewModel;
            if (vm != null)
            {
                vm.LoadCategories();
            }
        }
    }
}
