(() => {
  "use strict";

  const initializeEnvironmentSelector = () => {
    const selector = document.querySelector("[data-environment-selector]");
    const form = document.getElementById("filtros-graficas");
    if (!selector || !form) {
      return;
    }

    const toggle = selector.querySelector(".environment-selector-toggle");
    const label = selector.querySelector("[data-environment-selector-label]");
    const error = form.querySelector("[data-environment-selector-error]");
    const checkboxes = [...selector.querySelectorAll("[data-environment-checkbox]")];
    const selectAllButton = selector.querySelector("[data-environment-select-all]");
    const clearButton = selector.querySelector("[data-environment-clear]");

    const selectedCheckboxes = () => checkboxes.filter(checkbox => checkbox.checked);
    const update = () => {
      const selected = selectedCheckboxes();
      if (selected.length === checkboxes.length && checkboxes.length > 0) {
        label.textContent = "Todos los ambientes";
      } else if (selected.length === 1) {
        label.textContent = selected[0].closest("label")
          ?.querySelector(".flex-fill")?.textContent.trim() ?? "1 ambiente seleccionado";
      } else if (selected.length > 1) {
        label.textContent = `${selected.length} ambientes seleccionados`;
      } else {
        label.textContent = "Ningún ambiente seleccionado";
      }

      if (selected.length > 0) {
        toggle.classList.remove("is-invalid");
        error.classList.remove("d-block");
      }
    };

    selectAllButton?.addEventListener("click", () => {
      checkboxes.forEach(checkbox => { checkbox.checked = true; });
      update();
    });

    clearButton?.addEventListener("click", () => {
      checkboxes.forEach(checkbox => { checkbox.checked = false; });
      update();
    });

    checkboxes.forEach(checkbox => checkbox.addEventListener("change", update));
    form.addEventListener("submit", event => {
      if (selectedCheckboxes().length > 0 || checkboxes.length === 0) {
        return;
      }

      event.preventDefault();
      toggle.classList.add("is-invalid");
      error.classList.add("d-block");
      toggle.focus();
    });

    update();
  };

  initializeEnvironmentSelector();

  const visualizationModes = [...document.querySelectorAll("[data-visualization-mode]")];
  const periodSelector = document.querySelector("[data-period-selector]");
  const updatePeriodVisibility = () => {
    const selectedMode = visualizationModes.find(option => option.checked)?.value;
    periodSelector?.classList.toggle("d-none", selectedMode === "DetalleHorarios");
  };
  visualizationModes.forEach(option => option.addEventListener("change", updatePeriodVisibility));
  updatePeriodVisibility();

  const dataElement = document.getElementById("datos-graficas");
  if (!dataElement || typeof Chart === "undefined") {
    return;
  }

  const charts = JSON.parse(dataElement.textContent || "[]");
  const dangerColor = "#d63939";
  const limitColor = "#626976";
  const colorsByEnvironment = new Map(
    charts.flatMap(chart => chart.puntos.map(point => [point.ambienteId, point.colorAmbiente]))
  );

  const formatValue = value => new Intl.NumberFormat("es-PE", {
    maximumFractionDigits: 2
  }).format(value);
  const visibleValueLabelsPlugin = {
    id: "visibleValueLabels",
    afterDatasetsDraw(chart, _args, options) {
      if (!options.enabled) {
        return;
      }

      const { ctx, chartArea } = chart;
      ctx.save();
      chart.data.datasets.forEach((dataset, datasetIndex) => {
        if (dataset.isLimit || !chart.isDatasetVisible(datasetIndex)) {
          return;
        }

        const metadata = chart.getDatasetMeta(datasetIndex);
        metadata.data.forEach((element, dataIndex) => {
          const value = dataset.data[dataIndex];
          if (value === null || value === undefined) {
            return;
          }

          const text = formatValue(value);
          const direction = datasetIndex % 2 === 0 ? -1 : 1;
          const row = Math.floor(datasetIndex / 2);
          const desiredY = element.y + direction * (13 + row * 12);
          const labelY = Math.max(chartArea.top + 8, Math.min(chartArea.bottom - 8, desiredY));
          ctx.font = "600 10px sans-serif";
          ctx.textAlign = "center";
          ctx.textBaseline = "middle";
          const width = ctx.measureText(text).width + 8;
          const height = 16;
          const left = Math.max(
            chartArea.left,
            Math.min(chartArea.right - width, element.x - width / 2)
          );

          ctx.fillStyle = "rgba(255, 255, 255, 0.92)";
          ctx.fillRect(left, labelY - height / 2, width, height);
          ctx.strokeStyle = dataset.borderColor;
          ctx.lineWidth = 1;
          ctx.strokeRect(left, labelY - height / 2, width, height);
          ctx.fillStyle = dataset.borderColor;
          ctx.fillText(text, left + width / 2, labelY);
        });
      });
      ctx.restore();
    }
  };

  for (const chartData of charts) {
    const canvas = document.querySelector(
      `[data-medicion-chart="${chartData.tipoMedicionId}"]`
    );
    if (!canvas || chartData.puntos.length === 0) {
      continue;
    }

    const axisKeys = chartData.etiquetas.map(item => item.clave);
    const labels = chartData.etiquetas.map(item => item.etiqueta);
    const environments = [...new Map(
      chartData.puntos.map(point => [point.ambienteId, point.ambiente])
    ).entries()].sort((left, right) => left[1].localeCompare(right[1], "es"));

    const datasets = environments.map(([environmentId, environmentName]) => {
      const points = chartData.puntos
        .filter(point => point.ambienteId === environmentId);
      const pointsByKey = new Map(points.map(point => [point.claveEje, point]));
      const alignedPoints = axisKeys.map(key => pointsByKey.get(key) ?? null);
      const color = colorsByEnvironment.get(environmentId);
      const isOutOfRange = point => point?.cantidadFueraDeRango > 0;
      const isIncomplete = point => point && point.cantidadRegistros < point.cantidadEsperada;

      return {
        label: environmentName,
        data: alignedPoints.map(point => point?.valor ?? null),
        pointMetadata: alignedPoints,
        borderColor: color,
        backgroundColor: color,
        pointBackgroundColor: alignedPoints.map(point =>
          isOutOfRange(point) ? dangerColor : isIncomplete(point) ? "#ffffff" : color
        ),
        pointBorderColor: alignedPoints.map(point =>
          isOutOfRange(point) ? dangerColor : color
        ),
        pointBorderWidth: 2,
        pointStyle: alignedPoints.map(point =>
          isOutOfRange(point) ? "triangle" : isIncomplete(point) ? "rectRot" : "circle"
        ),
        pointRadius: alignedPoints.map(point =>
          !point ? 0 : isOutOfRange(point) || isIncomplete(point) ? 6 : 4
        ),
        pointHoverRadius: 6,
        borderWidth: 2,
        tension: 0,
        spanGaps: false
      };
    });

    const ranges = new Map(
      chartData.puntos.map(point => [
        `${point.limiteMinimo}|${point.limiteMaximo}`,
        [point.limiteMinimo, point.limiteMaximo]
      ])
    );
    if (ranges.size === 1) {
      const [[minimum, maximum]] = ranges.values();
      datasets.push(
        {
          label: `Límite mínimo (${formatValue(minimum)} ${chartData.unidad})`,
          data: labels.map(() => minimum),
          isLimit: true,
          borderColor: limitColor,
          backgroundColor: limitColor,
          borderDash: [6, 5],
          borderWidth: 1,
          pointRadius: 0,
          pointHoverRadius: 0
        },
        {
          label: `Límite máximo (${formatValue(maximum)} ${chartData.unidad})`,
          data: labels.map(() => maximum),
          isLimit: true,
          borderColor: limitColor,
          backgroundColor: limitColor,
          borderDash: [6, 5],
          borderWidth: 1,
          pointRadius: 0,
          pointHoverRadius: 0
        }
      );
    }

    new Chart(canvas, {
      type: "line",
      data: { labels, datasets },
      plugins: [visibleValueLabelsPlugin],
      options: {
        responsive: true,
        maintainAspectRatio: false,
        animation: { duration: 250 },
        interaction: {
          mode: "nearest",
          intersect: false
        },
        scales: {
          x: {
            grid: { display: false },
            ticks: {
              autoSkip: true,
              maxRotation: 45,
              minRotation: 0,
              maxTicksLimit: chartData.esPromedioDiario ? 16 : 8
            },
            title: {
              display: true,
              text: chartData.esPromedioDiario ? "Día operativo" : "Horario"
            }
          },
          y: {
            grace: chartData.esPromedioDiario ? "4%" : "14%",
            ticks: {
              count: 16,
              padding: 2,
              font: { size: 10 },
              callback: value => formatValue(value)
            },
            title: {
              display: true,
              text: chartData.unidad
            }
          }
        },
        plugins: {
          visibleValueLabels: {
            enabled: !chartData.esPromedioDiario
          },
          legend: {
            position: "bottom",
            labels: {
              boxWidth: 12,
              boxHeight: 12,
              padding: 18,
              usePointStyle: true
            }
          },
          tooltip: {
            filter: context => !context.dataset.isLimit,
            callbacks: {
              label: context => {
                const point = context.dataset.pointMetadata?.[context.dataIndex];
                if (!point) {
                  return "";
                }

                const description = point.esPromedio ? "Promedio" : "Valor";
                return `${point.ambiente} · ${description}: ${formatValue(point.valor)} ${chartData.unidad}`;
              },
              afterLabel: context => {
                const point = context.dataset.pointMetadata?.[context.dataIndex];
                if (!point) {
                  return [];
                }

                const details = [];
                if (point.esPromedio) {
                  details.push(
                    `Mínimo: ${formatValue(point.valorMinimo)} ${chartData.unidad}`,
                    `Máximo: ${formatValue(point.valorMaximo)} ${chartData.unidad}`,
                    `Registros: ${point.cantidadRegistros} de ${point.cantidadEsperada}`
                  );
                } else if (point.horario) {
                  details.push(`Horario: ${point.horario}`);
                }

                details.push(
                  `Rango aplicado: ${formatValue(point.limiteMinimo)} – ${formatValue(point.limiteMaximo)} ${chartData.unidad}`,
                  point.cantidadFueraDeRango > 0
                    ? `Alertas de rango: ${point.cantidadFueraDeRango}`
                    : "Sin alertas de rango",
                  `Día operativo: ${point.fechaOperativa}`
                );
                return details;
              }
            }
          }
        }
      }
    });
  }

  document
    .querySelectorAll('[data-bs-toggle="tab"][data-bs-target^="#grafica-panel-"]')
    .forEach(tab => tab.addEventListener("shown.bs.tab", event => {
      const targetSelector = event.target.getAttribute("data-bs-target");
      const panel = targetSelector ? document.querySelector(targetSelector) : null;
      panel?.querySelectorAll("canvas").forEach(canvas => {
        Chart.getChart(canvas)?.resize();
      });
    }));
})();
