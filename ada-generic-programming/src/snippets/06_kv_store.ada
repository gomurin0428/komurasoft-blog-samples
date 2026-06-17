-- 06_kv_store.ada
-- Generic key-value store package with multiple formal parameters.
-- Demonstrates how generics compose: type, operator, and behaviour.
-- Uses Unbounded_String for the value type since String is indefinite.

generic
   type Key_Type is private;
   type Value_Type is private;
   with function "=" (Left, Right : Key_Type) return Boolean is <>;
   Max_Entries : Positive := 50;
package Generic_KV_Store is
   procedure Put (Key : Key_Type; Val : Value_Type);
   function Get (Key : Key_Type) return Value_Type;
   function Contains (Key : Key_Type) return Boolean;
    Key_Not_Found : exception;
    Store_Full    : exception;
end Generic_KV_Store;

package body Generic_KV_Store is
   type KV_Entry is record
      Key  : Key_Type;
      Val  : Value_Type;
      Used : Boolean := False;
   end record;
   type Store_Array is array (1 .. Max_Entries) of KV_Entry;

   Store : Store_Array;

   procedure Put (Key : Key_Type; Val : Value_Type) is
   begin
      for I in Store'Range loop
         if not Store (I).Used or else Store (I).Key = Key then
            Store (I) := (Key => Key, Val => Val, Used => True);
            return;
         end if;
      end loop;
      raise Store_Full;
   end Put;

   function Get (Key : Key_Type) return Value_Type is
   begin
      for I in Store'Range loop
         if Store (I).Used and then Store (I).Key = Key then
            return Store (I).Val;
         end if;
      end loop;
      raise Key_Not_Found;
   end Get;

   function Contains (Key : Key_Type) return Boolean is
   begin
      for I in Store'Range loop
         if Store (I).Used and then Store (I).Key = Key then
            return True;
         end if;
      end loop;
      return False;
   end Contains;
end Generic_KV_Store;

with Ada.Text_IO;               use Ada.Text_IO;
with Ada.Strings.Unbounded;      use Ada.Strings.Unbounded;
with Generic_KV_Store;

procedure KV_Store_Demo is
   package Int_Store is new Generic_KV_Store (Integer, Unbounded_String, Max_Entries => 10);

   use Int_Store;
begin
   Put (1, To_Unbounded_String ("Ada"));
   Put (2, To_Unbounded_String ("GNAT"));
   Put (3, To_Unbounded_String ("SPARK"));
   Put_Line ("Key 1: " & To_String (Get (1)));
   Put_Line ("Key 2: " & To_String (Get (2)));
   Put_Line ("Key 3: " & To_String (Get (3)));
   Put_Line ("Has key 2? " & Boolean'Image (Contains (2)));
   Put_Line ("Has key 9? " & Boolean'Image (Contains (9)));
end KV_Store_Demo;
