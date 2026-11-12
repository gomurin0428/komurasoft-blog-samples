# Σ_F: 旧スキーマのデータ db（最初の例）を新スキーマへ移す
Σ = SigmaMigrationFunctor(F, Orders{String, Int, Int}, OrdersV2{String, Int, Int})
# 値の決まらない列を AttrVar で表せる圏を、移す先として渡す
cat = ACSetCategory(VarACSetCat(OrdersV2{String, Int, Int}()))
moved = Σ(db; cat)
println("地域: ", nparts(moved, :Region), " 件")
println("顧客 → 地域: ", moved[:region])
println("地域名: ", moved[:region_name])
println("顧客名: ", moved[:customer_name])
println("受注 → 顧客: ", moved[:customer])

# Σ で移したデータを Δ で旧スキーマに戻すと、元の db と一致する
back = migrate(Orders{String, Int, Int}, moved, DeltaMigration(F))
println("Δ で戻すと元のデータと同じか: ", back == db)
