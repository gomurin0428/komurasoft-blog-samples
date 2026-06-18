-- 02_stack.ada
-- Generic package: a bounded stack parameterised by element type and capacity.
-- Demonstrates generic formal private type and numeric discriminant.

generic
   type Element_Type is private;
   Max_Size : Positive;
package Generic_Stack is
   procedure Push (Item : Element_Type);
   function Pop return Element_Type;
   function Is_Empty return Boolean;
   function Is_Full return Boolean;
   function Size return Natural;
   Stack_Overflow  : exception;
   Stack_Underflow : exception;
end Generic_Stack;

package body Generic_Stack is
   type Element_Array is array (1 .. Max_Size) of Element_Type;
   Store : Element_Array;
   Top   : Natural := 0;

   procedure Push (Item : Element_Type) is
   begin
      if Top = Max_Size then
         raise Stack_Overflow;
      end if;
      Top                 := Top + 1;
      Store (Top)         := Item;
   end Push;

   function Pop return Element_Type is
   begin
      if Top = 0 then
         raise Stack_Underflow;
      end if;
      Top := Top - 1;
      return Store (Top + 1);
   end Pop;

   function Is_Empty return Boolean is (Top = 0);
   function Is_Full  return Boolean is (Top = Max_Size);
   function Size     return Natural is (Top);
end Generic_Stack;

with Ada.Text_IO;       use Ada.Text_IO;
with Generic_Stack;

procedure Stack_Demo is
   package Int_Stack is new Generic_Stack (Integer, 5);
   package Float_Stack is new Generic_Stack (Float, 3);

   use Int_Stack;
begin
   Push (42);
   Push (100);
   Push (-7);
   Put_Line ("Int stack size: " & Natural'Image (Size));
   Put_Line ("Popped: " & Integer'Image (Pop));
   Put_Line ("Popped: " & Integer'Image (Pop));
   Put_Line ("Int stack size: " & Natural'Image (Size));

   Float_Stack.Push (3.14);
   Float_Stack.Push (2.71);
   Put_Line ("Float popped: " & Float'Image (Float_Stack.Pop));
end Stack_Demo;
