using Sims3.SimIFace;
using Sims3.UI;
using System;
using Destrospean.Delegates;

namespace Sims3.Gameplay.Destrospean.Utils
{
    public class DebugUtils
    {
        [Tunable]
        static bool kShowDebugMessages;

        /// <summary>
        /// Shows a debug message dialog (only when <see cref="Sims3.Gameplay.Destrospean.Utils.DebugUtils.kShowDebugMessages"/> is set to <c>true</c>).
        /// </summary>
        public static void ShowDebugMessageDialog(string message)
        {
            if (kShowDebugMessages)
            {
                SimpleMessageDialog.Show("Debug Message", message);
            }
        }

        /// <summary>
        /// Shows a debug message notification (only when <see cref="Sims3.Gameplay.Destrospean.Utils.DebugUtils.kShowDebugMessages"/> is set to <c>true</c>).
        /// </summary>
        public static void ShowDebugMessageNotification(string message)
        {
            if (kShowDebugMessages)
            {
                StyledNotification.Show(new StyledNotification.Format(message, StyledNotification.NotificationStyle.kDebugAlert));
            }
        }

        /// <summary>
        /// Displays a script error in a the script error window if one is found.
        /// </summary>
        /// <returns><c>true</c>, if a script error was found, <c>false</c> otherwise.</returns>
        /// <param name="action">Function to execute and check for an error.</param>
        public static bool TryDisplayScriptError(Action action)
        {
            Exception exception;
            if (TryGetException(action, out exception))
            {
                ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, exception);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Displays a script error in a the script error window if one is found.
        /// </summary>
        /// <returns><c>true</c>, if a script error was found, <c>false</c> otherwise.</returns>
        /// <param name="callback">Function to execute and check for an error.</param>
        /// <param name="value">Return value of the callback function.</param>
        /// <typeparam name="T">Return value type.</typeparam>
        public static bool TryDisplayScriptError<T>(Func<T> callback, out T value)
        {
            T retVal = default(T);
            bool errorDisplayed = TryDisplayScriptError(() => retVal = callback());
            value = retVal;
            return errorDisplayed;
        }

        public static bool TryGetException(Action action, out Exception exception)
        {
            try
            {
                exception = null;
                action();
                return false;
            }
            catch (Exception ex)
            {
                exception = ex;
                return true;
            }
        }
    }
}
