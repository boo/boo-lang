import System.Collections.Generic

actual = LinkedList<int>().GetType()
expected = typeof(LinkedList).BindGenericParameters((int,))
assert actual is expected
