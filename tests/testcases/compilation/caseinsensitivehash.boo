"""
bar
True
"""
import System
import System.Globalization

h = Hash(StringComparer.Create(CultureInfo.CurrentCulture, true))
h["foo"] = "bar"
print(h["fOO"])
print("FOO" in h)
