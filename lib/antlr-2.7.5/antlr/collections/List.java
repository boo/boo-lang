package antlr.collections;

/* ANTLR Translator Generator
 * Project led by Terence Parr at http://www.jGuru.com
 * Software rights: http://www.antlr.org/license.html
 *
 * $Id: //depot/code/org.antlr/release/antlr-2.7.5/antlr/collections/List.java#1 $
 */

import java.util.Enumeration;
import java.util.NoSuchElementException;

/**A simple List interface that describes operations
 * on a list.
 */
public interface List {
    public void add(Object o); // can insert at head or append.

    public void append(Object o);

    public Object elementAt(int index) throws NoSuchElementException;

    public Enumeration elements();

    public boolean includes(Object o);

    public int length();
}
