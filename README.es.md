# SQLBasic.net

> [Inglés](README.md) | [Japonés](README.ja.md) | [Chino simplificado](README.zh-CN.md) | **Español**

**SQLBasic.net** es un editor SQL intuitivo para **SQLite**.
Ofrece resaltado de sintaxis, autocompletado inteligente y formato de SQL, permitiéndote ver los resultados de las consultas al instante en la misma ventana.
Al funcionar completamente **sin conexión y sin necesidad de nube**, la configuración es mínima y puedes comenzar a aprender inmediatamente tras la instalación.

<img width="1234" height="617" alt="image" src="https://github.com/user-attachments/assets/068f58dd-5b3d-4a50-bc96-1b9a6593e69c" />

## Usuarios objetivo

Esta aplicación está diseñada para **principiantes que están comenzando a aprender SQL**.
No se requiere ninguna instalación ni configuración de base de datos.

Al iniciarse, se genera automáticamente un archivo SQLite dedicado, por lo que puedes crear tablas y experimentar con operaciones SQL básicas de inmediato.
El resaltado de sintaxis y el autocompletado inteligente ayudan a detectar errores de forma temprana, brindando una experiencia fluida incluso para usuarios que lo utilizan por primera vez.
Como la aplicación funciona completamente **sin conexión**, puedes estudiar a tu propio ritmo sin depender de una red.

## Requisitos del sistema

- Windows 10 / 11
- .NET 8.0 (escritorio de Windows)

## Características

### Editor

| Característica | Atajo | Descripción |
|---|---|---|
| Resaltado de sintaxis | — | Las palabras clave SQL, cadenas, números y comentarios se muestran en colores distintos |
| Formato SQL | `Alt+F` | Formatea el documento completo |
| Comentar línea | `Ctrl+K` | Agrega `-- ` al inicio de la línea actual o de todas las líneas seleccionadas |
| Descomentar línea | `Shift+Ctrl+K` | Elimina `-- ` del inicio de la línea actual o de todas las líneas seleccionadas |
| Ejecución según cursor | `Ctrl+Enter` | Ejecuta solo la consulta en la posición del cursor, usando punto y coma como delimitador |
| Exportar CSV | `Ctrl+L` | Exporta el resultado de la consulta actual a un archivo CSV |

### Autocompletado inteligente (`Ctrl+Space`)

El autocompletado es consciente del contexto y sugiere distintos candidatos según la posición del cursor.

| Contexto | Candidatos |
|---|---|
| Después de `FROM` / `JOIN` / `UPDATE` / `INSERT INTO` | Nombres de tabla |
| Después de `DROP TABLE` / `ALTER TABLE` / `CREATE TABLE` | Nombres de tabla (soporta `IF EXISTS` / `IF NOT EXISTS`) |
| En la lista de columnas de `SELECT` | Nombres de columnas de todas las tablas referenciadas en la consulta |
| Después de `tabla.` | Nombres de columnas de esa tabla específica |
| Después de las columnas de SELECT (antes de `FROM`) | `FROM` |
| Al inicio de una nueva sentencia | `SELECT`, `INSERT INTO`, `UPDATE`, `DELETE FROM`, `CREATE TABLE`, etc. |
| Después del nombre de tabla en `FROM` | `WHERE`, `INNER JOIN`, `LEFT JOIN`, `GROUP BY`, `ORDER BY`, `LIMIT`, etc. |
| Después de `WHERE` / `AND` / `OR` | `AND`, `OR`, `NOT`, `EXISTS`, `BETWEEN`, `IN`, `LIKE`, `IS`, `NULL` |
| Después de `GROUP` / `ORDER` | `BY` |
| Después de la columna en `ORDER BY` | `ASC`, `DESC`, `NULLS FIRST`, `NULLS LAST` |
| Después de `INNER` / `LEFT` / `RIGHT` / `FULL` | `JOIN`, `OUTER JOIN` |
| Después de `LIMIT` | `OFFSET` |
| Después de `CREATE` | `TABLE`, `INDEX`, `VIEW`, `TRIGGER`, `TEMP TABLE` |
| Después de `DROP` | `TABLE`, `INDEX`, `VIEW`, `TRIGGER` |

### Mensajes de resultado de ejecución

Tras ejecutar una consulta, se muestra un mensaje en la barra de estado en la parte inferior de la ventana.

| Operación | Mensaje |
|---|---|
| `SELECT` | Número de filas devueltas |
| `INSERT` | Número de filas insertadas |
| `UPDATE` | Número de filas actualizadas |
| `DELETE` | Número de filas eliminadas |
| `CREATE TABLE` / `CREATE INDEX` | Tipo y nombre del objeto creado |
| `DROP TABLE` / `DROP INDEX` | Tipo y nombre del objeto eliminado |

### Panel de base de datos (barra lateral derecha)

- **Lista de tablas** — Muestra todas las tablas de la base de datos SQLite conectada
- **Información de columnas** — Selecciona una tabla para ver sus columnas, tipos de datos y estado de nulabilidad
- **Conectar a otra BD** — Cambia a un archivo SQLite diferente en cualquier momento

### Otras funciones

- **Gestión de historial** — Navega y vuelve a ejecutar consultas anteriores
- **Plantillas de consultas** — Reutiliza fragmentos SQL de uso frecuente
- **Múltiples ventanas** — Abre varias ventanas del editor para comparar consultas
- **Creación automática de BD** — Se crea automáticamente un archivo SQLite al primer inicio

## Atajos de teclado

| Atajo | Acción |
|---|---|
| `Ctrl+Enter` | Ejecutar consulta en la posición del cursor |
| `Ctrl+Space` | Abrir autocompletado |
| `Ctrl+K` | Agregar comentario de línea (`-- `) |
| `Shift+Ctrl+K` | Eliminar comentario de línea |
| `Alt+F` | Formatear documento SQL |
| `Ctrl+L` | Exportar resultados a CSV |

## Cómo iniciar

Al iniciarse, el programa crea automáticamente un archivo SQLite en una ruta fija.
Luego puedes escribir y ejecutar consultas SQL directamente en el editor integrado.

Para conectar a un archivo SQLite diferente, haz clic en el botón **"Conectar a otra BD"** en la barra lateral derecha.
