-- 03_sort.ada
-- Generic subprogram with a formal subprogram parameter.
-- Instantiate the same sort logic for different element types and orderings.

generic
   type Item_Type is private;
   type Index is (<>);
   type Item_Array is array (Index range <>) of Item_Type;
   with function "<" (Left, Right : Item_Type) return Boolean is <>;
procedure Generic_Insertion_Sort (Items : in out Item_Array);

procedure Generic_Insertion_Sort (Items : in out Item_Array) is
   Temp : Item_Type;
   J    : Index;
begin
   for I in Index'Succ (Items'First) .. Items'Last loop
      Temp := Items (I);
      J    := I;
      while J > Items'First and then Temp < Items (Index'Pred (J)) loop
         Items (J) := Items (Index'Pred (J));
         J := Index'Pred (J);
      end loop;
      Items (J) := Temp;
   end loop;
end Generic_Insertion_Sort;

with Ada.Text_IO;                use Ada.Text_IO;
with Generic_Insertion_Sort;

procedure Sort_Demo is
   type Int_Array is array (Positive range <>) of Integer;

   function Greater (Left, Right : Integer) return Boolean is
      (Left > Right);

   procedure Sort_Asc  is new Generic_Insertion_Sort (Integer, Positive, Int_Array);
   procedure Sort_Desc is new Generic_Insertion_Sort (Integer, Positive, Int_Array, "<" => Greater);

   Data_Asc  : Int_Array := (99, 3, 47, 12, 55, 8);
   Data_Desc : Int_Array := (99, 3, 47, 12, 55, 8);

   procedure Print (Label : String; Arr : Int_Array) is
   begin
      Put (Label & ":");
      for V of Arr loop
         Put (Integer'Image (V));
      end loop;
      New_Line;
   end Print;
begin
   Print ("Original", Data_Asc);
   Sort_Asc (Data_Asc);
   Print ("Ascending", Data_Asc);
   Sort_Desc (Data_Desc);
   Print ("Descending", Data_Desc);
end Sort_Demo;
