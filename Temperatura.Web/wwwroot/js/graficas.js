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

  const dataElement = document.getElementById("datos-graficas");
  if (!dataElement || typeof Chart === "undefined") {
    return;
  }

  const charts = JSON.parse(dataElement.textContent || "[]");
  const colorsByEnvironment = new Map(
    charts.flatMap(chart => chart.puntos.map(point => [point.ambienteId, point.colorAmbiente]))
  );

  const formatValue = value => new Intl.NumberFormat("es-PE", {
    maximumFractionDigits: 2
  }).format(value);
  const resizeChartFrame = canvas => {
    const scroll = canvas.closest("[data-chart-scroll]");
    const frame = canvas.closest("[data-chart-frame]");
    const columnCount = Number.parseInt(canvas.dataset.columnCount ?? "0", 10);
    if (!scroll || !frame || !columnCount) {
      return;
    }

    const availableWidth = scroll.clientWidth || scroll.parentElement?.clientWidth || 0;
    frame.style.width = `${Math.max(availableWidth, columnCount * 64)}px`;
  };
  const visibleValueLabelsPlugin = {
    id: "visibleValueLabels",
    afterDatasetsDraw(chart, _args, options) {
      if (!options.enabled) {
        return;
      }

      const { ctx, chartArea } = chart;
      ctx.save();
      let seriesIndex = 0;
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
          const direction = seriesIndex % 2 === 0 ? -1 : 1;
          const row = Math.floor(seriesIndex / 2);
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
        seriesIndex += 1;
      });
      ctx.restore();
    }
  };
  const htmlLegendPlugin = {
    id: "htmlLegend",
    afterUpdate(chart) {
      const legend = chart.canvas
        .closest("[role='tabpanel']")
        ?.querySelector(`[data-chart-legend="${chart.canvas.dataset.medicionChart}"]`);
      if (!legend) {
        return;
      }

      const items = chart.options.plugins.legend.labels
        .generateLabels(chart)
        .filter(item => chart.data.datasets[item.datasetIndex]?.showInLegend !== false);
      legend.replaceChildren();
      items.forEach(item => {
        const dataset = chart.data.datasets[item.datasetIndex];
        const button = document.createElement("button");
        button.type = "button";
        button.className = "chart-legend-item";
        button.classList.toggle("is-hidden", item.hidden);
        button.style.setProperty("--chart-legend-color", item.strokeStyle);
        button.setAttribute("aria-pressed", String(!item.hidden));
        button.title = `${item.hidden ? "Mostrar" : "Ocultar"} ${item.text}`;

        const swatch = document.createElement("span");
        swatch.className = "chart-legend-swatch";
        swatch.setAttribute("aria-hidden", "true");

        const label = document.createElement("span");
        label.textContent = item.text;
        button.append(swatch, label);
        button.addEventListener("click", () => {
          const visible = !chart.isDatasetVisible(item.datasetIndex);
          chart.data.datasets.forEach((candidate, datasetIndex) => {
            if (candidate.environmentId === dataset.environmentId) {
              chart.setDatasetVisibility(datasetIndex, visible);
            }
          });
          chart.update();
        });
        legend.append(button);
      });
    }
  };
  const fixedYAxisPlugin = {
    id: "fixedYAxis",
    afterDraw(chart, _args, options) {
      if (!options.enabled) {
        return;
      }

      const axis = chart.canvas
        .closest(".chart-layout")
        ?.querySelector("[data-fixed-y-axis]");
      const yScale = chart.scales.y;
      if (!axis || !yScale) {
        return;
      }

      axis.style.height = `${chart.height}px`;
      axis.replaceChildren();

      const unit = document.createElement("span");
      unit.className = "chart-fixed-axis-unit";
      unit.style.top = `${(yScale.top + yScale.bottom) / 2}px`;
      unit.textContent = options.unit ?? "";
      axis.append(unit);

      yScale.ticks.forEach((tick, index) => {
        const label = document.createElement("span");
        label.className = "chart-fixed-axis-value";
        label.style.top = `${yScale.getPixelForTick(index)}px`;
        label.textContent = formatValue(tick.value);
        axis.append(label);
      });
    }
  };
  const groupedColumnsPlugin = {
    id: "groupedColumns",
    beforeDatasetsDraw(chart) {
      const columnCount = chart.data.labels.length;
      const { ctx, chartArea } = chart;
      if (!columnCount || !chartArea) {
        return;
      }

      const groupedAxis = chart.canvas
        .closest("[data-chart-frame]")
        ?.querySelector(".chart-grouped-axis");
      if (groupedAxis) {
        groupedAxis.style.setProperty("--chart-plot-left", `${chartArea.left}px`);
        groupedAxis.style.setProperty(
          "--chart-plot-right",
          `${chart.width - chartArea.right}px`
        );
      }

      const columnWidth = (chartArea.right - chartArea.left) / columnCount;
      ctx.save();
      for (let index = 0; index <= columnCount; index += 1) {
        const isDayBoundary = index % 4 === 0;
        const x = chartArea.left + columnWidth * index;
        ctx.beginPath();
        ctx.moveTo(x, chartArea.top);
        ctx.lineTo(x, chartArea.bottom);
        ctx.strokeStyle = isDayBoundary
          ? "rgba(98, 105, 118, 0.35)"
          : "rgba(98, 105, 118, 0.12)";
        ctx.lineWidth = isDayBoundary ? 1.25 : 0.5;
        ctx.stroke();
      }
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
    const labels = [...axisKeys];
    canvas.dataset.columnCount = axisKeys.length.toString();
    resizeChartFrame(canvas);
    const environments = [...new Map(
      chartData.puntos.map(point => [point.ambienteId, point.ambiente])
    ).entries()].sort((left, right) => left[1].localeCompare(right[1], "es"));

    const limitDatasets = [];
    const datasets = environments.map(([environmentId, environmentName]) => {
      const points = chartData.puntos
        .filter(point => point.ambienteId === environmentId);
      const pointsByKey = new Map(points.map(point => [point.claveEje, point]));
      const alignedPoints = axisKeys.map(key => pointsByKey.get(key) ?? null);
      const color = colorsByEnvironment.get(environmentId);
      const isOutOfRange = point =>
        point?.estadoRango === "PorDebajo" || point?.estadoRango === "PorEncima";

      limitDatasets.push(
        {
          label: `${environmentName} · límite mínimo`,
          data: alignedPoints.map(point => point?.limiteMinimo ?? null),
          environmentId,
          isLimit: true,
          showInLegend: false,
          borderColor: color,
          backgroundColor: color,
          borderDash: [6, 5],
          borderWidth: 1.25,
          pointRadius: 0,
          pointHoverRadius: 0,
          spanGaps: true,
          stepped: "after",
          order: 1
        },
        {
          label: `${environmentName} · límite máximo`,
          data: alignedPoints.map(point => point?.limiteMaximo ?? null),
          environmentId,
          isLimit: true,
          showInLegend: false,
          borderColor: color,
          backgroundColor: color,
          borderDash: [6, 5],
          borderWidth: 1.25,
          pointRadius: 0,
          pointHoverRadius: 0,
          spanGaps: true,
          stepped: "after",
          order: 1
        }
      );

      return {
        label: environmentName,
        data: alignedPoints.map(point => point?.valor ?? null),
        environmentId,
        pointMetadata: alignedPoints,
        borderColor: color,
        backgroundColor: color,
        pointBackgroundColor: color,
        pointBorderColor: color,
        pointBorderWidth: 2,
        pointStyle: alignedPoints.map(point =>
          isOutOfRange(point) ? "triangle" : "circle"
        ),
        pointRotation: alignedPoints.map(point =>
          point?.estadoRango === "PorDebajo" ? 180 : 0
        ),
        pointRadius: alignedPoints.map(point =>
          !point ? 0 : isOutOfRange(point) ? 6 : 4
        ),
        pointHoverRadius: 6,
        borderWidth: 2,
        tension: 0,
        spanGaps: true
      };
    });
    datasets.push(...limitDatasets);

    new Chart(canvas, {
      type: "line",
      data: { labels, datasets },
      plugins: [
        groupedColumnsPlugin,
        visibleValueLabelsPlugin,
        htmlLegendPlugin,
        fixedYAxisPlugin
      ],
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
            display: false,
            offset: true
          },
          y: {
            grace: "12%",
            afterFit: scale => { scale.width = 0; },
            ticks: {
              display: false,
              count: 16,
              padding: 0
            },
            border: { display: false },
            title: {
              display: false
            }
          }
        },
        plugins: {
          visibleValueLabels: {
            enabled: axisKeys.length <= 16
          },
          fixedYAxis: {
            enabled: true,
            unit: chartData.unidad
          },
          legend: {
            display: false,
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
              title: items => {
                const column = chartData.etiquetas[items[0]?.dataIndex];
                return column ? `${column.fecha} · ${column.momento}` : "";
              },
              label: context => {
                const point = context.dataset.pointMetadata?.[context.dataIndex];
                if (!point) {
                  return "";
                }

                return `${point.ambiente} · Valor: ${formatValue(point.valor)} ${chartData.unidad}`;
              },
              afterLabel: context => {
                const point = context.dataset.pointMetadata?.[context.dataIndex];
                if (!point) {
                  return [];
                }

                const details = [
                  `Momento: ${point.momento}`,
                  `Horario: ${point.horario} (${point.horaReferencia})`
                ];

                if (point.esDiaSiguiente) {
                  details.push("La toma ocurre al día calendario siguiente.");
                }

                details.push(
                  `Rango aplicado: ${formatValue(point.limiteMinimo)} – ${formatValue(point.limiteMaximo)} ${chartData.unidad}`,
                  point.estadoRango === "PorDebajo"
                    ? "Por debajo del mínimo"
                    : point.estadoRango === "PorEncima"
                      ? "Por encima del máximo"
                      : "Dentro de rango",
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
        resizeChartFrame(canvas);
        Chart.getChart(canvas)?.resize();
      });
    }));
})();
