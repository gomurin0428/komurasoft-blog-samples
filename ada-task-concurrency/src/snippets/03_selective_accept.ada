-- 03_selective_accept.ada
-- Selective accept: a server task can wait for multiple entries simultaneously.
-- The first caller to arrive gets served first; an `or` branch with `else` prevents blocking.

with Ada.Text_IO; use Ada.Text_IO;

procedure Selective_Accept_Demo is

   task Server is
      entry Deposit  (Amount : Integer);
      entry Withdraw (Amount : Integer; Success : out Boolean);
      entry Balance  (Value : out Integer);
   end Server;

   task body Server is
      Current : Integer := 0;
   begin
      loop
         select
            accept Deposit (Amount : Integer) do
               Current := Current + Amount;
               Put_Line ("  [Server] Deposited" & Integer'Image (Amount)
                         & ", balance =" & Integer'Image (Current));
            end Deposit;
         or
            accept Withdraw (Amount : Integer; Success : out Boolean) do
               if Current >= Amount then
                  Current := Current - Amount;
                  Success := True;
                  Put_Line ("  [Server] Withdrew" & Integer'Image (Amount)
                            & ", balance =" & Integer'Image (Current));
               else
                  Success := False;
                  Put_Line ("  [Server] Withdraw" & Integer'Image (Amount)
                            & " failed (insufficient funds)");
               end if;
            end Withdraw;
         or
            accept Balance (Value : out Integer) do
               Value := Current;
               Put_Line ("  [Server] Balance requested =" & Integer'Image (Current));
            end Balance;
         or
            terminate;
         end select;
      end loop;
   end Server;

   Success : Boolean;
   Bal     : Integer;
begin
   Put_Line ("Main: depositing 100...");
   Server.Deposit (100);

   Put_Line ("Main: withdrawing 30...");
   Server.Withdraw (30, Success);
   Put_Line ("Main: withdrawal success = " & Boolean'Image (Success));

   Put_Line ("Main: checking balance...");
   Server.Balance (Bal);
   Put_Line ("Main: final balance =" & Integer'Image (Bal));
end Selective_Accept_Demo;