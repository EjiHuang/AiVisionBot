using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents.DocumentStructures;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MainView.Framework
{
    public class UIHelper
    {
        public static T FindChild<T>(DependencyObject parent, string childName)
           where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        public static IEnumerable<T> FindChild<T>(DependencyObject parent)
        where T : DependencyObject
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                var childType = child as T;
                if (childType != null)
                {
                    yield return (T)child;
                }

                foreach (var other in FindChild<T>(child))
                {
                    yield return other;
                }
            }
        }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            // get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            // we’ve reached the end of the tree
            if (parentObject == null) return null;

            // check if the parent matches the type we’re looking for
            if (parentObject is T parent)
            {
                return parent;
            }
            else
            {
                // use recursion to proceed with next level
                return FindParent<T>(parentObject);
            }
        }

        public static void AnimateOpacity(DependencyObject target, double from, double to)
        {
            var opacityAnimation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(500)
            };

            Storyboard.SetTarget(opacityAnimation, target);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));

            var storyboard = new Storyboard();
            storyboard.Children.Add(opacityAnimation);
            storyboard.Begin();
        }

        public static Storyboard AnimateRotate(DependencyObject target, bool isRepeat = false)
        {
            var rotateAnimation = new DoubleAnimation()
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromMilliseconds(500),
                RepeatBehavior = isRepeat == true ? RepeatBehavior.Forever : new RepeatBehavior(1)
            };

            Storyboard.SetTarget(rotateAnimation, target);
            Storyboard.SetTargetProperty(rotateAnimation, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));

            var storyboard = new Storyboard();
            storyboard.Children.Add(rotateAnimation);

            return storyboard;
        }
    }
}
