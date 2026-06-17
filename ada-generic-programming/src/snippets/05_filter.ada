-- 05_filter.ada
-- Generic function that accepts a predicate as a formal subprogram parameter.
-- Demonstrates higher-order generics: passing behaviour as a template argument.

generic
   type Element is private;
   type Index is (<>);
   type Array_Type is array (Index range <>) of Element;
   with function Predicate (Item : Element) return Boolean is <>;
function Generic_Count_If (Arr : Array_Type) return Natural;

function Generic_Count_If (Arr : Array_Type) return Natural is
   Count : Natural := 0;
begin
   for I in Arr'Range loop
      if Predicate (Arr (I)) then
         Count := Count + 1;
      end if;
   end loop;
   return Count;
end Generic_Count_If;

with Ada.Text_IO;         use Ada.Text_IO;
with Generic_Count_If;

procedure Filter_Demo is
   type Int_Array is array (Positive range <>) of Integer;

   function Is_Even (N : Integer) return Boolean is (N mod 2 = 0);
   function Is_Large (N : Integer) return Boolean is (N > 50);

   function Count_Even is new Generic_Count_If (Integer, Positive, Int_Array, Predicate => Is_Even);
   function Count_Large is new Generic_Count_If (Integer, Positive, Int_Array, Predicate => Is_Large);

   Data : Int_Array := (12, 7, 88, 3, 56, 91, 44, 19, 62);

   procedure Print (Label : String; Arr : Int_Array) is
   begin
      Put (Label & ": [");
      for I in Arr'Range loop
         Put (Integer'Image (Arr (I)));
         if I < Arr'Last then
            Put (",");
         end if;
      end loop;
      Put_Line (" ]");
   end Print;
begin
   Print ("Data  ", Data);
   Put_Line ("Even count: " & Natural'Image (Count_Even (Data)));
   Put_Line (">50  count: " & Natural'Image (Count_Large (Data)));
end Filter_Demo;
