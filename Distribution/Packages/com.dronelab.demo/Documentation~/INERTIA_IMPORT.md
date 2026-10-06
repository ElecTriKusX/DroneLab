# Полный тензор инерции из CAD / измерений

Полный тензор влияет на angular acceleration и связь вращения вокруг разных осей.
Старая AutoBox оценивает однородный параллелепипед; для аппарата с вынесенной батареей,
камерой и моторами измеренный/CAD tensor предпочтительнее геометрической заглушки.
Внешний mesh без материалов и распределения масс не даёт достоверную инерцию.

Импорт ожидает **actual symmetric tensor about COM**, в локальных осях physics root:
Unity +X right, +Y up, +Z forward; COM в метрах; масса kg; инерция kg*m².
CAD-отчёт относительно origin нужно сначала привести к COM; Z-up переориентировать;
mm/g конвертировать; products of inertia могут иметь противоположный знак actual
off-diagonal tensor. Автоматического угадывания этого нет.

Пример (только синтетическая проверка): [Examples/cad_inertia_test.json](Examples/cad_inertia_test.json).
Матрица включает Ixz=Izx=0.002 kg*m²; не вводить это значение в свой FPV без измерения.

1. Stop Play → выделить корень с DronePhysicsBody и текущим Drone Profile.
2. `DroneLab → Mass Properties → Import CAD Inertia Tensor`.
3. Выбрать JSON с корректно приведёнными CAD данными.
4. Сохранить новый drone JSON; он назначается автоматически. Сохранить сцену → Play.

Импорт заменяет massKg/COM/inertia; сохраняет rotors/aero/battery/settings и удаляет
stale derived cache. Если COM marker уже существует, он перемещается в новый COM,
чтобы следующий geometry export не вернул старое значение. Drag Point остаётся
на прежнем месте (а если его нет в JSON — fallback теперь новый COM).

Core Jacobi eigensolver разлагает I=R diag(I1,I2,I3) Rᵀ. Нормализация масштаба сохраняет
точность малых тензоров; eigenvalues сортируются; basis имеет determinant +1;
quaternion unit. Проверяются finite/symmetry/SPD и physical triangle inequalities.
Повторные eigenvalues допустимы: orientation главных осей неоднозначна, но tensor тот же.

Результат сохраняется в существующий ManualPrincipal + principalAxesRotationXyzw.
Версия и семантика drone JSON 1.0.0 не меняются. Import-document version 1.0.0 является
самостоятельным форматом authoring, не новым runtime режимом. Форму внешнего
CAD JSON проверяет CadInertiaImport; итог проходит обычный ProfileLoader со schema.

При смене массы/COM нужно проверить controller allocation и gains. Инерция сама не
калибрует моторы, аэродинамику или регуляторы. Сборка mass properties из отдельных
компонентов по Штейнеру остаётся следующим возможным authoring инструментом.
