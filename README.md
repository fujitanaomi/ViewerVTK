# KasandraViewerVTK
Demo técnica de un visor interactivo desarrollado en **WPF + VTK** para visualizar resultados de simulación generados por **OpenFOAM**. Este visor fue desarrollado como parte de una propuesta técnica presentada a una compañía española en 2025 para la versión 2 de una aplicación ya existente.

## 🚀 Funcionalidades principales
- 📂 Carga de archivos `.vtu` binarios generados desde OpenFOAM.
- ⚡ Visualización fluida incluso con mallas grandes (hasta 250 millones de celdas).
- ✂️ Corte interactivo a través de plano deslizable.
- 🎨 Coloreado por escalares seleccionables desde el dataset.
- 🎚️ Filtro por rango escalar (threshold).
- 📊 Leyenda automática de colores.

## 📦 Requisitos
- **.NET 8 Desktop Runtime**
- **ActiViz.NET 9.4 (versión de evaluación)**  
  > La DLL de `Kitware.VTK` utilizada en este proyecto corresponde a la **versión 9.4 demo** descargada directamente desde la página oficial de Kitware.  
  > Esta versión tiene limitaciones, pero es válida para pruebas técnicas no comerciales.
  
## 🧰 Tecnologías utilizadas
- **.NET 8**  
- **WPF (.NET Desktop)**  
- **ActiViz.NET 9.4** (demo de VTK para .NET)  
- **VTK 9.x**  
- **OpenFOAM** (para generación de archivos `.vtu`)

## 🧑‍💻 Sobre mí
Mi nombre es Naomi, soy desarrolladora freelance especializada en .NET, interfaces fluidas y optimización visual para entornos técnicos e industriales.

📬 [Contáctame por LinkedIn][(https://www.linkedin.com/in/naomi-fujita-salinas-0a463167/)  

**Licencia:** Este código es de uso educativo y de portfolio. No se permite su uso comercial sin autorización previa.
