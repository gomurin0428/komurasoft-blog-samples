-- 01_swap.ada
-- Basic generic subprogram: a single Swap procedure works for any type.
-- Demonstrates generic formal private type and instantiation.

generic
   type Element is private;
procedure Generic_Swap (A, B : in out Element);

procedure Generic_Swap (A, B : in out Element) is
   Temp : constant Element := A;
begin
   A := B;
   B := Temp;
end Generic_Swap;

with Ada.Text_IO;       use Ada.Text_IO;
with Generic_Swap;

procedure Swap_Demo is
   procedure Swap_Int is new Generic_Swap (Integer);
   procedure Swap_Char is new Generic_Swap (Character);

   X : Integer := 10;
   Y : Integer := 20;
   C : Character := 'A';
   D : Character := 'B';
begin
   Put_Line ("Before: X =" & Integer'Image (X) & ", Y =" & Integer'Image (Y));
   Swap_Int (X, Y);
   Put_Line ("After : X =" & Integer'Image (X) & ", Y =" & Integer'Image (Y));

   Put_Line ("Before: C =" & Character'Image (C) & ", D =" & Character'Image (D));
   Swap_Char (C, D);
   Put_Line ("After : C =" & Character'Image (C) & ", D =" & Character'Image (D));
end Swap_Demo;
