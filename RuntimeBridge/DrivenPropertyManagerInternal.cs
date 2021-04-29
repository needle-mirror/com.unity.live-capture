using UnityEngine;

namespace Unity.LiveCapture.Internal
{
    static class DrivenPropertyManagerInternal
    {
        public static void RegisterProperty(Object driver, Object target, string propertyPath)
        {
            DrivenPropertyManager.TryRegisterProperty(driver, target, propertyPath);
        }

        public static void UnregisterProperties(Object driver)
        {
            DrivenPropertyManager.UnregisterProperties(driver);
        }
    }
}
